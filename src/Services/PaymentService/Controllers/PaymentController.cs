using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Services;
using System.Net;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<PaymentController> _logger;
    private readonly IPaymentSagaService _paymentSagaService;

    public PaymentController(PaymentDbContext context, ILogger<PaymentController> logger, IPaymentSagaService paymentSagaService)
    {
        _context = context;
        _logger = logger;
        _paymentSagaService = paymentSagaService;
    }

    [HttpPost("process")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { error = "Invalid request body" });
            }

            // Validate request
            if (request.Amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than 0" });
            }

            if (request.DonationId <= 0)
            {
                return BadRequest(new { error = "Invalid donation ID" });
            }

            _logger.LogInformation("Starting payment processing for donation {DonationId} with amount {Amount}",
                request.DonationId, request.Amount);

            // For now, we'll simulate the orchestration process
            // TODO: Implement proper payment saga orchestration
            var transactionId = GenerateTransactionId();

            var response = new
            {
                orchestrationId = transactionId,
                donationId = request.DonationId,
                amount = request.Amount,
                status = "Processing",
                statusCheckUrl = $"/api/payments/status/{transactionId}"
            };

            // Queue payment for processing with saga service
            _paymentSagaService.QueuePaymentForProcessing(request, transactionId);

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment request");
            return StatusCode((int)HttpStatusCode.InternalServerError,
                new { error = "An error occurred while processing the payment" });
        }
    }

    [HttpGet("status/{instanceId}")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
    public IActionResult GetPaymentStatus(string instanceId)
    {
        try
        {
            // For now, return a mock status
            // TODO: Implement proper status tracking
            var response = new
            {
                orchestrationId = instanceId,
                runtimeStatus = "Completed",
                createdTime = DateTime.UtcNow.AddMinutes(-5),
                lastUpdatedTime = DateTime.UtcNow,
                output = new { status = "Success" }
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting payment status for instance {InstanceId}: {Error}",
                instanceId, ex.Message);
            return StatusCode((int)HttpStatusCode.InternalServerError,
                new { error = "An error occurred while retrieving payment status" });
        }
    }

    [HttpGet("{transactionId}")]
    [ProducesResponseType(typeof(PaymentStatusResponse), (int)HttpStatusCode.OK)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> GetPaymentByTransactionId(string transactionId)
    {
        try
        {
            var payment = await _context.PaymentTransactions
                .Include(p => p.LedgerEntries)
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return NotFound(new { error = $"Payment transaction {transactionId} not found" });
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

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment by transaction ID {TransactionId}", transactionId);
            return StatusCode((int)HttpStatusCode.InternalServerError,
                new { error = "An error occurred while retrieving the payment" });
        }
    }

    [HttpPost("{transactionId}/refund")]
    [ProducesResponseType(typeof(RefundResponse), (int)HttpStatusCode.Accepted)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.NotFound)]
    public async Task<IActionResult> RefundPayment(string transactionId, [FromBody] RefundRequest? refundRequest)
    {
        try
        {
            if (refundRequest == null)
            {
                return BadRequest(new { error = "Invalid request body" });
            }

            var payment = await _context.PaymentTransactions
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

            if (payment == null)
            {
                return NotFound(new { error = $"Payment transaction {transactionId} not found" });
            }

            if (payment.Status != PaymentStatus.Completed)
            {
                return BadRequest(new { error = "Only completed payments can be refunded" });
            }

            var refundAmount = refundRequest.Amount ?? payment.Amount;

            if (refundAmount <= 0 || refundAmount > payment.Amount)
            {
                return BadRequest(new { error = "Invalid refund amount" });
            }

            // In a real implementation, this would start a refund process
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

            return Accepted(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", transactionId);
            return StatusCode((int)HttpStatusCode.InternalServerError,
                new { error = "An error occurred while processing the refund" });
        }
    }

    [HttpGet("donation/{donationId}")]
    [ProducesResponseType(typeof(List<PaymentResponse>), (int)HttpStatusCode.OK)]
    public async Task<IActionResult> GetPaymentsByDonation(int donationId)
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

            return Ok(payments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments for donation {DonationId}", donationId);
            return StatusCode((int)HttpStatusCode.InternalServerError,
                new { error = "An error occurred while retrieving payments" });
        }
    }

    [HttpGet("info")]
    [ProducesResponseType(typeof(object), (int)HttpStatusCode.OK)]
    public IActionResult GetServiceInfo()
    {
        var info = new
        {
            service = "PaymentService",
            version = "1.0.0",
            description = "Payment processing service",
            features = new[]
            {
                "Payment processing",
                "Payment status tracking",
                "Refund processing",
                "Payment ledger",
                "Distributed locking"
            },
            timestamp = DateTime.UtcNow
        };

        return Ok(info);
    }


    private static string GenerateTransactionId()
    {
        return $"TXN-PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
