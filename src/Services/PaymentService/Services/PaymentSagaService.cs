using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.DTOs;
using PaymentService.Models;
using PaymentService.Services;
using PaymentService.Events;
using System.Collections.Concurrent;

namespace PaymentService.Services;

public class PaymentSagaService : BackgroundService, IPaymentSagaService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentSagaService> _logger;
    private readonly ConcurrentQueue<PaymentProcessingRequest> _paymentQueue = new();

    public PaymentSagaService(IServiceProvider serviceProvider, ILogger<PaymentSagaService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void QueuePaymentForProcessing(ProcessPaymentRequest request, string transactionId)
    {
        _paymentQueue.Enqueue(new PaymentProcessingRequest
        {
            Request = request,
            TransactionId = transactionId,
            QueuedAt = DateTime.UtcNow
        });

        _logger.LogInformation("Queued payment {TransactionId} for processing", transactionId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Payment Saga Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_paymentQueue.TryDequeue(out var paymentRequest))
            {
                await ProcessPaymentAsync(paymentRequest, stoppingToken);
            }
            else
            {
                await Task.Delay(1000, stoppingToken); // Wait 1 second before checking again
            }
        }

        _logger.LogInformation("Payment Saga Service is stopping");
    }

    private async Task ProcessPaymentAsync(PaymentProcessingRequest paymentRequest, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var paymentGateway = scope.ServiceProvider.GetRequiredService<IPaymentGatewayService>();
        var distributedLock = scope.ServiceProvider.GetRequiredService<IDistributedLockService>();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var transactionId = paymentRequest.TransactionId;
        var request = paymentRequest.Request;

        try
        {
            _logger.LogInformation("Starting payment saga for donation {DonationId} with transaction {TransactionId}",
                request.DonationId, transactionId);

            // Step 1: Acquire distributed lock to prevent duplicate payments
            var lockKey = $"payment:{request.DonationId}:{request.PaymentMethod}";
            var lockResult = await distributedLock.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(10));

            if (lockResult == null)
            {
                _logger.LogWarning("Failed to acquire lock for donation {DonationId}", request.DonationId);
                await UpdatePaymentStatusAsync(context, transactionId, PaymentStatus.Failed,
                    "Another payment is already in progress for this donation");
                return;
            }

            try
            {
                // Step 2: Create payment transaction record
                var paymentTransaction = new PaymentTransaction
                {
                    TransactionId = transactionId,
                    DonationId = request.DonationId,
                    CampaignId = request.CampaignId,
                    Amount = request.Amount,
                    Currency = request.Currency,
                    PaymentMethod = request.PaymentMethod,
                    Status = PaymentStatus.Processing,
                    CreatedAt = DateTime.UtcNow,
                    PaymentGateway = string.Empty,
                    GatewayTransactionId = string.Empty,
                    FailureReason = string.Empty,
                    ProcessedAt = null,
                    CompletedAt = null
                };

                context.PaymentTransactions.Add(paymentTransaction);
                await context.SaveChangesAsync(cancellationToken);

                // Step 3: Process payment with gateway
                var paymentResult = await paymentGateway.ProcessPaymentAsync(request, transactionId);

                // Step 4: Update payment status based on gateway response
                paymentTransaction.Status = paymentResult.IsSuccess ? PaymentStatus.Completed : PaymentStatus.Failed;
                paymentTransaction.GatewayTransactionId = paymentResult.GatewayTransactionId;
                paymentTransaction.PaymentGateway = paymentResult.PaymentGateway;
                paymentTransaction.FailureReason = paymentResult.ErrorMessage ?? string.Empty;
                paymentTransaction.ProcessedAt = DateTime.UtcNow;

                if (paymentResult.IsSuccess)
                {
                    paymentTransaction.CompletedAt = DateTime.UtcNow;
                }

                await context.SaveChangesAsync(cancellationToken);

                // Step 5: Create ledger entries
                await CreateLedgerEntriesAsync(context, paymentTransaction.Id, transactionId, paymentResult);

                // Step 6: Publish events to outbox
                if (paymentResult.IsSuccess)
                {
                    await outboxService.PublishEventAsync(new PaymentCompletedEvent
                    {
                        TransactionId = transactionId,
                        DonationId = request.DonationId,
                        CampaignId = request.CampaignId,
                        Amount = request.Amount,
                        Currency = request.Currency,
                        PaymentMethod = request.PaymentMethod,
                        DonorName = request.DonorName,
                        DonorEmail = request.DonorEmail,
                        CompletedAt = DateTime.UtcNow,
                        GatewayTransactionId = paymentResult.GatewayTransactionId
                    });
                }
                else
                {
                    await outboxService.PublishEventAsync(new PaymentFailedEvent
                    {
                        TransactionId = transactionId,
                        DonationId = request.DonationId,
                        CampaignId = request.CampaignId,
                        Amount = request.Amount,
                        PaymentMethod = request.PaymentMethod,
                        DonorName = request.DonorName,
                        DonorEmail = request.DonorEmail,
                        FailureReason = paymentResult.ErrorMessage ?? "Unknown error",
                        FailedAt = DateTime.UtcNow
                    });
                }

                _logger.LogInformation("Completed payment saga for transaction {TransactionId} with status {Status}",
                    transactionId, paymentTransaction.Status);
            }
            finally
            {
                // Always release the lock
                if (lockResult != null)
                {
                    await lockResult.ReleaseLockAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment saga failed for transaction {TransactionId}", transactionId);

            // Update payment status to failed
            await UpdatePaymentStatusAsync(context, transactionId, PaymentStatus.Failed,
                $"Saga execution failed: {ex.Message}");
        }
    }

    private async Task UpdatePaymentStatusAsync(PaymentDbContext context, string transactionId,
        PaymentStatus status, string? failureReason = null)
    {
        var payment = await context.PaymentTransactions
            .FirstOrDefaultAsync(p => p.TransactionId == transactionId);

        if (payment != null)
        {
            payment.Status = status;
            payment.FailureReason = failureReason;
            payment.ProcessedAt = DateTime.UtcNow;

            if (status == PaymentStatus.Completed)
            {
                payment.CompletedAt = DateTime.UtcNow;
            }

            await context.SaveChangesAsync();
        }
    }

    private async Task CreateLedgerEntriesAsync(PaymentDbContext context, int paymentTransactionId, string transactionId,
        PaymentGatewayResponse paymentResult)
    {
        var entries = new List<PaymentLedgerEntry>();

        // Payment entry for the payment amount
        entries.Add(new PaymentLedgerEntry
        {
            PaymentTransactionId = paymentTransactionId,
            TransactionId = transactionId,
            Amount = paymentResult.Amount,
            EntryType = LedgerEntryType.Payment,
            Operation = "Payment",
            Description = $"Payment processed via {paymentResult.PaymentGateway}",
            CreatedAt = DateTime.UtcNow
        });

        // Fee entry for the gateway fee (if any)
        if (paymentResult.GatewayFee > 0)
        {
            entries.Add(new PaymentLedgerEntry
            {
                PaymentTransactionId = paymentTransactionId,
                TransactionId = transactionId,
                Amount = paymentResult.GatewayFee,
                EntryType = LedgerEntryType.Fee,
                Operation = "Gateway Fee",
                Description = $"Gateway fee for {paymentResult.PaymentGateway}",
                CreatedAt = DateTime.UtcNow
            });
        }

        context.PaymentLedgerEntries.AddRange(entries);
        await context.SaveChangesAsync();
    }
}

public class PaymentProcessingRequest
{
    public ProcessPaymentRequest Request { get; set; } = null!;
    public string TransactionId { get; set; } = null!;
    public DateTime QueuedAt { get; set; }
}
