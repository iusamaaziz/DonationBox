using System.Text.Json;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Activities;

public class PaymentSagaActivities
{
    private readonly PaymentDbContext _context;
    private readonly IDistributedLockService _lockService;
    private readonly IPaymentGatewayService _gatewayService;
    private readonly IOutboxService _outboxService;
    private readonly ILogger<PaymentSagaActivities> _logger;

    public PaymentSagaActivities(
        PaymentDbContext context,
        IDistributedLockService lockService,
        IPaymentGatewayService gatewayService,
        IOutboxService outboxService,
        ILogger<PaymentSagaActivities> logger)
    {
        _context = context;
        _lockService = lockService;
        _gatewayService = gatewayService;
        _outboxService = outboxService;
        _logger = logger;
    }

    [Function(nameof(AcquireSagaLockActivity))]
    public async Task<SagaLockState?> AcquireSagaLockActivity([ActivityTrigger] AcquireSagaLockInput input)
    {
        try
        {
            var lockExpiration = TimeSpan.FromMinutes(15); // Initial lock expires in 15 minutes
            var waitTime = TimeSpan.FromSeconds(10); // Wait up to 10 seconds

            var sagaLock = await _lockService.AcquireSagaLockAsync(
                input.SagaInstanceId,
                input.DonationId.ToString(),
                input.PaymentMethod,
                input.Amount,
                lockExpiration,
                waitTime);
            
            if (sagaLock == null)
            {
                _logger.LogWarning("Failed to acquire saga lock for donation {DonationId}, method {PaymentMethod}, amount {Amount}", 
                    input.DonationId, input.PaymentMethod, input.Amount);
                return null;
            }

            _logger.LogInformation("Acquired saga lock {LockKey} for donation {DonationId} with transaction {TransactionId} and saga {SagaInstanceId}", 
                sagaLock.LockKey, input.DonationId, input.TransactionId, input.SagaInstanceId);
            
            return sagaLock;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring saga lock for donation {DonationId} and saga {SagaInstanceId}", 
                input.DonationId, input.SagaInstanceId);
            return null;
        }
    }

    [Function(nameof(ExtendSagaLockActivity))]
    public async Task<bool> ExtendSagaLockActivity([ActivityTrigger] ExtendSagaLockInput input)
    {
        try
        {
            var extended = await _lockService.ExtendSagaLockAsync(input.LockState, input.Extension);
            
            if (extended)
            {
                _logger.LogInformation("Extended saga lock {LockKey} for saga {SagaInstanceId} by {Extension}", 
                    input.LockState.LockKey, input.LockState.SagaInstanceId, input.Extension);
            }
            else
            {
                _logger.LogWarning("Failed to extend saga lock {LockKey} for saga {SagaInstanceId}", 
                    input.LockState.LockKey, input.LockState.SagaInstanceId);
            }

            return extended;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extending saga lock {LockKey} for saga {SagaInstanceId}", 
                input.LockState.LockKey, input.LockState.SagaInstanceId);
            return false;
        }
    }

    [Function(nameof(ReleaseSagaLockActivity))]
    public async Task<bool> ReleaseSagaLockActivity([ActivityTrigger] ReleaseSagaLockInput input)
    {
        try
        {
            var released = await _lockService.ReleaseSagaLockAsync(input.LockState);
            
            if (released)
            {
                _logger.LogInformation("Released saga lock {LockKey} for saga {SagaInstanceId}", 
                    input.LockState.LockKey, input.LockState.SagaInstanceId);
            }
            else
            {
                _logger.LogWarning("Failed to release saga lock {LockKey} for saga {SagaInstanceId} - it may have already expired", 
                    input.LockState.LockKey, input.LockState.SagaInstanceId);
            }

            return released;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing saga lock {LockKey} for saga {SagaInstanceId}", 
                input.LockState.LockKey, input.LockState.SagaInstanceId);
            return false;
        }
    }

    [Function(nameof(ValidateSagaLockActivity))]
    public async Task<bool> ValidateSagaLockActivity([ActivityTrigger] ValidateSagaLockInput input)
    {
        try
        {
            var isValid = await _lockService.IsSagaLockValidAsync(input.LockState);
            
            if (isValid)
            {
                _logger.LogDebug("Saga lock {LockKey} for saga {SagaInstanceId} is valid", 
                    input.LockState.LockKey, input.LockState.SagaInstanceId);
            }
            else
            {
                _logger.LogWarning("Saga lock {LockKey} for saga {SagaInstanceId} is no longer valid", 
                    input.LockState.LockKey, input.LockState.SagaInstanceId);
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating saga lock {LockKey} for saga {SagaInstanceId}", 
                input.LockState.LockKey, input.LockState.SagaInstanceId);
            return false;
        }
    }

    [Function(nameof(CreatePaymentTransactionActivity))]
    public async Task<bool> CreatePaymentTransactionActivity([ActivityTrigger] CreatePaymentTransactionInput input)

    {
        try
        {
            var transaction = new PaymentTransaction
            {
                TransactionId = input.TransactionId,
                DonationId = input.Request.DonationId,
                CampaignId = input.Request.CampaignId,
                Amount = input.Request.Amount,
                Currency = input.Request.Currency,
                DonorName = input.Request.DonorName,
                DonorEmail = input.Request.DonorEmail,
                Status = PaymentStatus.Pending,
                PaymentMethod = input.Request.PaymentMethod,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PaymentTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created payment transaction {TransactionId} for donation {DonationId}", 
                input.TransactionId, input.Request.DonationId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment transaction {TransactionId}", input.TransactionId);
            return false;
        }
    }

    [Function(nameof(ProcessPaymentWithGatewayActivity))]
    public async Task<PaymentGatewayResponse> ProcessPaymentWithGatewayActivity([ActivityTrigger] ProcessPaymentWithGatewayInput input)
    {

        try
        {
            _logger.LogInformation("Processing payment with gateway for transaction {TransactionId}", input.TransactionId);
            
            var response = await _gatewayService.ProcessPaymentAsync(input.Request, input.TransactionId);
            
            _logger.LogInformation("Gateway response for transaction {TransactionId}: {Status}", 
                input.TransactionId, response.Status);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment with gateway for transaction {TransactionId}", input.TransactionId);
            
            return new PaymentGatewayResponse
            {
                IsSuccess = false,
                Status = PaymentStatus.Failed,
                ErrorMessage = $"Gateway error: {ex.Message}",
                ProcessedAt = DateTime.UtcNow
            };
        }
    }

    [Function(nameof(UpdatePaymentStatusActivity))]
    public async Task<PaymentResponse> UpdatePaymentStatusActivity([ActivityTrigger] UpdatePaymentStatusInput input)
    {

        try
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == input.TransactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Payment transaction {input.TransactionId} not found");
            }

            transaction.Status = input.GatewayResponse.Status;
            transaction.PaymentGateway = GetGatewayName(transaction.PaymentMethod);
            transaction.GatewayTransactionId = input.GatewayResponse.GatewayTransactionId;
            transaction.ProcessedAt = input.GatewayResponse.ProcessedAt;
            transaction.UpdatedAt = DateTime.UtcNow;

            if (input.GatewayResponse.IsSuccess)
            {
                transaction.CompletedAt = input.GatewayResponse.ProcessedAt;
            }
            else
            {
                transaction.FailureReason = input.GatewayResponse.ErrorMessage ?? "Unknown error";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated payment transaction {TransactionId} status to {Status}", 
                input.TransactionId, input.GatewayResponse.Status);

            return new PaymentResponse
            {
                TransactionId = input.TransactionId,
                DonationId = transaction.DonationId,
                CampaignId = transaction.CampaignId,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                Status = transaction.Status,
                PaymentMethod = transaction.PaymentMethod,
                PaymentGateway = transaction.PaymentGateway,
                GatewayTransactionId = transaction.GatewayTransactionId,
                FailureReason = transaction.FailureReason,
                CreatedAt = transaction.CreatedAt,
                ProcessedAt = transaction.ProcessedAt,
                CompletedAt = transaction.CompletedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment status for transaction {TransactionId}", input.TransactionId);
            throw;
        }
    }

    [Function(nameof(CreateLedgerEntriesActivity))]
    public async Task CreateLedgerEntriesActivity([ActivityTrigger] CreateLedgerEntriesInput input)
    {

        try
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == input.TransactionId);

            if (transaction == null)
            {
                throw new InvalidOperationException($"Payment transaction {input.TransactionId} not found");
            }

            var ledgerEntries = new List<PaymentLedgerEntry>
            {
                // Payment entry
                new PaymentLedgerEntry
                {
                    PaymentTransactionId = transaction.Id,
                    TransactionId = input.TransactionId,
                    Amount = transaction.Amount,
                    EntryType = LedgerEntryType.Payment,
                    Operation = "DEBIT",
                    Description = $"Payment for donation {transaction.DonationId}",
                    Metadata = JsonSerializer.Serialize(input.GatewayResponse.Metadata),
                    CreatedAt = DateTime.UtcNow
                },
                // Processing fee entry
                new PaymentLedgerEntry
                {
                    PaymentTransactionId = transaction.Id,
                    TransactionId = $"{input.TransactionId}-FEE",
                    Amount = input.GatewayResponse.ProcessingFee,
                    EntryType = LedgerEntryType.Fee,
                    Operation = "DEBIT",
                    Description = "Payment processing fee",
                    CreatedAt = DateTime.UtcNow
                }
            };

            _context.PaymentLedgerEntries.AddRange(ledgerEntries);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created ledger entries for transaction {TransactionId}", input.TransactionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ledger entries for transaction {TransactionId}", input.TransactionId);
            throw;
        }
    }

    [Function(nameof(ConfirmDonationActivity))]
    public async Task<bool> ConfirmDonationActivity([ActivityTrigger] ConfirmDonationInput input)
    {

        try
        {
            // In a real implementation, this would call the DonationService API
            // For now, we'll simulate the confirmation
            _logger.LogInformation("Confirming donation {DonationId} with transaction {TransactionId} and status {Status}", 
                input.DonationId, input.TransactionId, input.Status);

            // Simulate API call delay
            await Task.Delay(500);

            // Simulate 95% success rate
            var random = new Random();
            var isSuccess = random.NextDouble() > 0.05;

            if (isSuccess)
            {
                _logger.LogInformation("Successfully confirmed donation {DonationId}", input.DonationId);
            }
            else
            {
                _logger.LogWarning("Failed to confirm donation {DonationId}", input.DonationId);
            }

            return isSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming donation {DonationId}", input.DonationId);
            return false;
        }
    }

    [Function(nameof(RefundPaymentActivity))]
    public async Task RefundPaymentActivity([ActivityTrigger] RefundPaymentInput input)
    {

        try
        {
            _logger.LogInformation("Processing refund for transaction {TransactionId}", input.TransactionId);

            var refundResponse = await _gatewayService.RefundPaymentAsync(input.GatewayTransactionId, input.Amount, input.Reason);

            // Update transaction status
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(t => t.TransactionId == input.TransactionId);

            if (transaction != null)
            {
                transaction.Status = refundResponse.Status;
                transaction.UpdatedAt = DateTime.UtcNow;

                // Create refund ledger entry
                var refundEntry = new PaymentLedgerEntry
                {
                    PaymentTransactionId = transaction.Id,
                    TransactionId = $"{input.TransactionId}-REFUND",
                    Amount = refundResponse.RefundedAmount,
                    EntryType = LedgerEntryType.Refund,
                    Operation = "CREDIT",
                    Description = $"Refund: {input.Reason}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.PaymentLedgerEntries.Add(refundEntry);
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Processed refund for transaction {TransactionId}: {Status}", 
                input.TransactionId, refundResponse.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", input.TransactionId);
            throw;
        }
    }

    [Function(nameof(PublishPaymentEventActivity))]
    public async Task PublishPaymentEventActivity([ActivityTrigger] object eventData)
    {
        try
        {
            await _outboxService.PublishEventAsync(eventData);
            _logger.LogInformation("Published payment event of type {EventType}", eventData.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing payment event of type {EventType}", eventData.GetType().Name);
            throw;
        }
    }

    private static string GetGatewayName(PaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            PaymentMethod.PayPal => "PayPal",
            PaymentMethod.BankTransfer => "ACH Network",
            PaymentMethod.ApplePay => "Apple Pay",
            PaymentMethod.GooglePay => "Google Pay",
            _ => "Stripe"
        };
    }
}
