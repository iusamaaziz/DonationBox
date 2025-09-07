using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

using PaymentService.Activities;
using PaymentService.DTOs;
using PaymentService.Events;
using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.Orchestrations;

public class PaymentSagaOrchestrator
{
    [Function(nameof(ProcessPaymentSaga))]
    public static async Task<PaymentResponse> ProcessPaymentSaga(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<ProcessPaymentRequest>()!;
        var transactionId = GenerateTransactionId();
        var sagaInstanceId = context.InstanceId;
        var logger = context.CreateReplaySafeLogger<PaymentSagaOrchestrator>();
        SagaLockState? sagaLock = null;

        logger.LogInformation("Starting payment saga for donation {DonationId} with transaction {TransactionId} and saga {SagaInstanceId}",
            request.DonationId, transactionId, sagaInstanceId);

        try
        {
            // Step 1: Acquire saga-scoped distributed lock to prevent duplicate payments
            sagaLock = await context.CallActivityAsync<SagaLockState?>(
                nameof(PaymentSagaActivities.AcquireSagaLockActivity),
                new AcquireSagaLockInput 
                { 
                    SagaInstanceId = sagaInstanceId,
                    DonationId = request.DonationId,
                    PaymentMethod = request.PaymentMethod.ToString(),
                    Amount = request.Amount,
                    TransactionId = transactionId 
                });

            if (sagaLock == null || !sagaLock.IsAcquired)
            {
                logger.LogWarning("Failed to acquire saga lock for donation {DonationId} - another payment may be in progress", request.DonationId);
                return new PaymentResponse
                {
                    TransactionId = transactionId,
                    Status = PaymentStatus.Failed,
                    FailureReason = "Duplicate payment detected - another payment is already in progress for this donation"
                };
            }

            logger.LogInformation("Successfully acquired saga lock {LockKey} for saga {SagaInstanceId}", sagaLock.LockKey, sagaInstanceId);

            // Step 2: Create payment transaction record
            var paymentCreated = await context.CallActivityAsync<bool>(
                nameof(PaymentSagaActivities.CreatePaymentTransactionActivity),
                new CreatePaymentTransactionInput { Request = request, TransactionId = transactionId });

            if (!paymentCreated)
            {
                return new PaymentResponse
                {
                    TransactionId = transactionId,
                    Status = PaymentStatus.Failed,
                    FailureReason = "Failed to create payment transaction"
                };
            }

            // Step 3: Validate lock before proceeding with payment gateway
            var lockValid = await context.CallActivityAsync<bool>(
                nameof(PaymentSagaActivities.ValidateSagaLockActivity),
                new ValidateSagaLockInput { LockState = sagaLock });

            if (!lockValid)
            {
                logger.LogWarning("Saga lock became invalid before payment processing for saga {SagaInstanceId}", sagaInstanceId);
                return new PaymentResponse
                {
                    TransactionId = transactionId,
                    Status = PaymentStatus.Failed,
                    FailureReason = "Payment lock expired during processing"
                };
            }

            // Step 4: Process payment with gateway
            var gatewayResponse = await context.CallActivityAsync<PaymentGatewayResponse>(
                nameof(PaymentSagaActivities.ProcessPaymentWithGatewayActivity),
                new ProcessPaymentWithGatewayInput { Request = request, TransactionId = transactionId });

            // Step 5: Update payment status based on gateway response
            var paymentResponse = await context.CallActivityAsync<PaymentResponse>(
                nameof(PaymentSagaActivities.UpdatePaymentStatusActivity),
                new UpdatePaymentStatusInput { TransactionId = transactionId, GatewayResponse = gatewayResponse });

            if (gatewayResponse.IsSuccess)
            {
                // Step 6: Extend lock before long-running operations
                var lockExtended = await context.CallActivityAsync<bool>(
                    nameof(PaymentSagaActivities.ExtendSagaLockActivity),
                    new ExtendSagaLockInput { LockState = sagaLock, Extension = TimeSpan.FromMinutes(10) });

                if (!lockExtended)
                {
                    logger.LogWarning("Failed to extend saga lock for saga {SagaInstanceId} - proceeding with caution", sagaInstanceId);
                }

                // Step 7: Create ledger entries
                await context.CallActivityAsync(
                    nameof(PaymentSagaActivities.CreateLedgerEntriesActivity),
                    new CreateLedgerEntriesInput { TransactionId = transactionId, GatewayResponse = gatewayResponse });

                // Step 8: Confirm donation with DonationService
                var donationConfirmed = await context.CallActivityAsync<bool>(
                    nameof(PaymentSagaActivities.ConfirmDonationActivity),
                    new ConfirmDonationInput
                    { 
                        DonationId = request.DonationId, 
                        TransactionId = transactionId,
                        Status = PaymentStatus.Completed
                    });

                if (!donationConfirmed)
                {
                    logger.LogWarning("Failed to confirm donation {DonationId}, initiating refund", request.DonationId);
                    
                    // Compensate: Refund the payment
                    await context.CallActivityAsync(
                        nameof(PaymentSagaActivities.RefundPaymentActivity),
                        new RefundPaymentInput
                        { 
                            TransactionId = transactionId,
                            GatewayTransactionId = gatewayResponse.GatewayTransactionId,
                            Amount = request.Amount,
                            Reason = "Failed to confirm donation"
                        });

                    paymentResponse.Status = PaymentStatus.Refunded;
                    paymentResponse.FailureReason = "Failed to confirm donation - payment refunded";
                }
                else
                {
                    // Step 9: Publish payment completed event
                    await context.CallActivityAsync(
                        nameof(PaymentSagaActivities.PublishPaymentEventActivity),
                        new PaymentCompletedEvent
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
                            GatewayTransactionId = gatewayResponse.GatewayTransactionId
                        });
                }
            }
            else
            {
                // Payment failed - publish failure event
                await context.CallActivityAsync(
                    nameof(PaymentSagaActivities.PublishPaymentEventActivity),
                    new PaymentFailedEvent
                    {
                        TransactionId = transactionId,
                        DonationId = request.DonationId,
                        CampaignId = request.CampaignId,
                        Amount = request.Amount,
                        PaymentMethod = request.PaymentMethod,
                        DonorName = request.DonorName,
                        DonorEmail = request.DonorEmail,
                        FailureReason = gatewayResponse.ErrorMessage ?? "Unknown error",
                        FailedAt = DateTime.UtcNow
                    });
            }

            logger.LogInformation("Completed payment saga for transaction {TransactionId} with status {Status}",
                transactionId, paymentResponse.Status);

            return paymentResponse;
        }
        catch (Exception ex)
        {
            logger.LogError("Payment saga failed for transaction {TransactionId}: {Error}", 
                transactionId, ex.Message);

            return new PaymentResponse
            {
                TransactionId = transactionId,
                Status = PaymentStatus.Failed,
                FailureReason = $"Saga execution failed: {ex.Message}"
            };
        }
        finally
        {
            // CRITICAL: Always release the saga lock, regardless of success or failure
            if (sagaLock != null && sagaLock.IsAcquired)
            {
                try
                {
                    var lockReleased = await context.CallActivityAsync<bool>(
                        nameof(PaymentSagaActivities.ReleaseSagaLockActivity),
                        new ReleaseSagaLockInput { LockState = sagaLock });

                    if (lockReleased)
                    {
                        logger.LogInformation("Successfully released saga lock {LockKey} for saga {SagaInstanceId}", 
                            sagaLock.LockKey, sagaInstanceId);
                    }
                    else
                    {
                        logger.LogWarning("Failed to release saga lock {LockKey} for saga {SagaInstanceId} - it may have already expired", 
                            sagaLock.LockKey, sagaInstanceId);
                    }
                }
                catch (Exception lockEx)
                {
                    logger.LogError("Exception occurred while releasing saga lock {LockKey} for saga {SagaInstanceId}: {Error}", 
                        sagaLock.LockKey, sagaInstanceId, lockEx.Message);
                }
            }
        }
    }

    private static string GenerateTransactionId()
    {
        return $"TXN-PAY-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
