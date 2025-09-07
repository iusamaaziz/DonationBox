using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.DurableTask.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Orchestrations;
using System.Net;
using System.Text.Json;

namespace PaymentService.Functions;

public class PaymentFunctions
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentFunctions> _logger;

    public PaymentFunctions(PaymentDbContext context, ILogger<PaymentFunctions> logger)
    {
        _context = context;
        _logger = logger;
    }

    [Function("ProcessPayment")]
    [OpenApiOperation(operationId: "ProcessPayment", tags: new[] { "Payments" }, Summary = "Process a donation payment", Description = "Initiates a payment saga orchestration to process a donation payment with distributed locking and reliable transaction handling.")]
    [OpenApiSecurity("ApiKey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(ProcessPaymentRequest), Required = true, Description = "Payment processing request containing donation details and payment information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(object), Summary = "Payment processing initiated", Description = "Returns orchestration details for tracking payment progress")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid request", Description = "Request validation failed")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Processing error", Description = "An error occurred during payment processing")]
    public async Task<HttpResponseData> ProcessPayment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "payments/process")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var paymentRequest = JsonSerializer.Deserialize<ProcessPaymentRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (paymentRequest == null)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadRequest, new { error = "Invalid request body" });
            }

            // Validate request
            if (paymentRequest.Amount <= 0)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadRequest, new { error = "Amount must be greater than 0" });
            }

            if (paymentRequest.DonationId <= 0)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadRequest, new { error = "Invalid donation ID" });
            }

            _logger.LogInformation("Starting payment processing for donation {DonationId} with amount {Amount}",
                paymentRequest.DonationId, paymentRequest.Amount);

            // Start the payment saga orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(PaymentSagaOrchestrator.ProcessPaymentSaga),
                paymentRequest);

            _logger.LogInformation("Started payment saga with instance ID {InstanceId} for donation {DonationId}",
                instanceId, paymentRequest.DonationId);

            // Return the orchestration instance ID for tracking
            var response = new
            {
                orchestrationId = instanceId,
                donationId = paymentRequest.DonationId,
                amount = paymentRequest.Amount,
                status = "Processing",
                statusCheckUrl = $"/api/payments/status/{instanceId}"
            };

            return await CreateResponseAsync(req, HttpStatusCode.Accepted, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request");
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError, 
                new { error = "An error occurred while processing the payment" });
        }
    }

    [Function("GetPaymentStatus")]
    [OpenApiOperation(operationId: "GetPaymentStatus", tags: new[] { "Payments" }, Summary = "Get payment orchestration status", Description = "Retrieves the current status of a payment saga orchestration using the orchestration instance ID.")]
    [OpenApiSecurity("ApiKey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
    [OpenApiParameter(name: "instanceId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Orchestration instance ID", Description = "The unique identifier of the payment saga orchestration instance")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Orchestration status", Description = "Returns the current status and details of the payment orchestration")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Orchestration not found", Description = "The specified orchestration instance was not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Status retrieval error", Description = "An error occurred while retrieving the orchestration status")]
    public async Task<HttpResponseData> GetPaymentStatus(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payments/status/{instanceId}")] HttpRequestData req,
        [DurableClient] DurableTaskClient client,
        string instanceId)
    {
        try
        {
            var status = await client.GetInstanceAsync(instanceId);
            
            if (status == null)
            {
                return await CreateResponseAsync(req, HttpStatusCode.NotFound, 
                    new { error = $"Payment orchestration {instanceId} not found" });
            }

            var response = new
            {
                orchestrationId = instanceId,
                runtimeStatus = status.RuntimeStatus.ToString(),
                createdTime = status.CreatedAt,
                lastUpdatedTime = status.LastUpdatedAt,
                output = status.SerializedOutput
            };

            return await CreateResponseAsync(req, HttpStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment status for instance {InstanceId}", instanceId);
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError, 
                new { error = "An error occurred while retrieving payment status" });
        }
    }

    [Function("GetPaymentByTransactionId")]
    [OpenApiOperation(operationId: "GetPaymentByTransactionId", tags: new[] { "Payments" }, Summary = "Get payment by transaction ID", Description = "Retrieves detailed payment information including ledger entries for a specific transaction ID.")]
    [OpenApiSecurity("ApiKey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
    [OpenApiParameter(name: "transactionId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Transaction ID", Description = "The unique identifier of the payment transaction")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PaymentStatusResponse), Summary = "Payment details", Description = "Returns detailed payment information with ledger entries")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Payment not found", Description = "The specified payment transaction was not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Retrieval error", Description = "An error occurred while retrieving the payment")]
    public async Task<HttpResponseData> GetPaymentByTransactionId(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payments/{transactionId}")] HttpRequestData req,
        string transactionId)
    {
        try
        {
            var payment = await _context.PaymentTransactions
                .Include(p => p.LedgerEntries)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return await CreateResponseAsync(req, HttpStatusCode.NotFound, 
                    new { error = $"Payment transaction {transactionId} not found" });
            }

            var response = new PaymentStatusResponse
            {
                TransactionId = payment.TransactionId,
                Status = payment.Status,
                Amount = payment.Amount,
                Currency = payment.Currency,
                CreatedAt = payment.CreatedAt,
                CompletedAt = payment.CompletedAt,
                LedgerEntries = payment.LedgerEntries.Select(e => new LedgerEntryResponse
                {
                    Amount = e.Amount,
                    EntryType = e.EntryType,
                    Operation = e.Operation,
                    Description = e.Description,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };

            return await CreateResponseAsync(req, HttpStatusCode.OK, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by transaction ID {TransactionId}", transactionId);
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError, 
                new { error = "An error occurred while retrieving the payment" });
        }
    }

    [Function("RefundPayment")]
    [OpenApiOperation(operationId: "RefundPayment", tags: new[] { "Payments" }, Summary = "Initiate payment refund", Description = "Initiates a refund process for a completed payment transaction. Supports full or partial refunds.")]
    [OpenApiSecurity("ApiKey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
    [OpenApiParameter(name: "transactionId", In = ParameterLocation.Path, Required = true, Type = typeof(string), Summary = "Transaction ID", Description = "The unique identifier of the payment transaction to refund")]
    [OpenApiRequestBody(contentType: "application/json", bodyType: typeof(RefundRequest), Required = true, Description = "Refund request details including amount and reason")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.Accepted, contentType: "application/json", bodyType: typeof(RefundResponse), Summary = "Refund initiated", Description = "Returns refund details and tracking information")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "application/json", bodyType: typeof(object), Summary = "Invalid refund request", Description = "Request validation failed or payment cannot be refunded")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "application/json", bodyType: typeof(object), Summary = "Payment not found", Description = "The specified payment transaction was not found")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Refund processing error", Description = "An error occurred while processing the refund")]
    public async Task<HttpResponseData> RefundPayment(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "payments/{transactionId}/refund")] HttpRequestData req,
        string transactionId)
    {
        try
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var refundRequest = JsonSerializer.Deserialize<RefundRequest>(requestBody, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (refundRequest == null)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadRequest, new { error = "Invalid request body" });
            }

            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return await CreateResponseAsync(req, HttpStatusCode.NotFound, 
                    new { error = $"Payment transaction {transactionId} not found" });
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadRequest, 
                    new { error = "Only completed payments can be refunded" });
            }

            var refundAmount = refundRequest.Amount ?? payment.Amount;
            
            if (refundAmount <= 0 || refundAmount > payment.Amount)
            {
                return await CreateResponseAsync(req, HttpStatusCode.BadRequest, 
                    new { error = "Invalid refund amount" });
            }

            // In a real implementation, this would start a refund orchestration
            // For now, we'll simulate the refund process
            var refundId = $"RF-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";

            _logger.LogInformation("Processing refund {RefundId} for transaction {TransactionId} with amount {Amount}",
                refundId, transactionId, refundAmount);

            var response = new RefundResponse
            {
                RefundId = refundId,
                OriginalTransactionId = transactionId,
                RefundAmount = refundAmount,
                Status = PaymentStatus.Processing,
                CreatedAt = DateTime.UtcNow
            };

            return await CreateResponseAsync(req, HttpStatusCode.Accepted, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError, 
                new { error = "An error occurred while processing the refund" });
        }
    }

    [Function("GetPaymentsByDonation")]
    [OpenApiOperation(operationId: "GetPaymentsByDonation", tags: new[] { "Payments" }, Summary = "Get payments by donation ID", Description = "Retrieves all payment transactions associated with a specific donation, ordered by creation date (newest first).")]
    [OpenApiSecurity("ApiKey", SecuritySchemeType.ApiKey, In = OpenApiSecurityLocationType.Header, Name = "x-api-key")]
    [OpenApiParameter(name: "donationId", In = ParameterLocation.Path, Required = true, Type = typeof(int), Summary = "Donation ID", Description = "The unique identifier of the donation")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(PaymentResponse[]), Summary = "Payment list", Description = "Returns a list of all payments for the specified donation")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "application/json", bodyType: typeof(object), Summary = "Retrieval error", Description = "An error occurred while retrieving payments")]
    public async Task<HttpResponseData> GetPaymentsByDonation(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "payments/donation/{donationId}")] HttpRequestData req,
        int donationId)
    {
        try
        {
            var payments = await _context.PaymentTransactions
                .Where(p => p.DonationId == donationId)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new PaymentResponse
                {
                    TransactionId = p.TransactionId,
                    DonationId = p.DonationId,
                    CampaignId = p.CampaignId,
                    Amount = p.Amount,
                    Currency = p.Currency,
                    Status = p.Status,
                    PaymentMethod = p.PaymentMethod,
                    PaymentGateway = p.PaymentGateway,
                    GatewayTransactionId = p.GatewayTransactionId,
                    FailureReason = p.FailureReason,
                    CreatedAt = p.CreatedAt,
                    ProcessedAt = p.ProcessedAt,
                    CompletedAt = p.CompletedAt
                })
                .ToListAsync();

            return await CreateResponseAsync(req, HttpStatusCode.OK, payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for donation {DonationId}", donationId);
            return await CreateResponseAsync(req, HttpStatusCode.InternalServerError, 
                new { error = "An error occurred while retrieving payments" });
        }
    }

    [Function("GetServiceInfo")]
    [OpenApiOperation(operationId: "GetServiceInfo", tags: new[] { "Health" }, Summary = "Get service information", Description = "Retrieves general information about the Payment Service including version, features, and capabilities.")]
    [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "application/json", bodyType: typeof(object), Summary = "Service information", Description = "Returns service metadata, version, and feature list")]
    public async Task<HttpResponseData> GetServiceInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "info")] HttpRequestData req)
    {
        var info = new
        {
            service = "PaymentService",
            version = "1.0.0",
            description = "Payment processing service with Durable Functions Saga orchestration",
            features = new[]
            {
                "Payment processing with multiple gateways",
                "Saga orchestration for reliable transactions",
                "Distributed locking for duplicate prevention",
                "Outbox pattern for reliable event delivery",
                "Payment ledger for audit trails"
            },
            timestamp = DateTime.UtcNow
        };

        return await CreateResponseAsync(req, HttpStatusCode.OK, info);
    }

    private static async Task<HttpResponseData> CreateResponseAsync(
        HttpRequestData req, 
        HttpStatusCode statusCode, 
        object content)
    {
        var response = req.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        
        var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        
        await response.WriteStringAsync(json);
        return response;
    }
}
