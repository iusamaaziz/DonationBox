using PaymentService.Models;
using PaymentService.Services;

namespace PaymentService.DTOs;

public class AcquireSagaLockInput
{
    public string SagaInstanceId { get; set; } = string.Empty;
    public int DonationId { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
}

public class ExtendSagaLockInput
{
    public SagaLockState LockState { get; set; } = new();
    public TimeSpan Extension { get; set; } = TimeSpan.FromMinutes(5);
}

public class ReleaseSagaLockInput
{
    public SagaLockState LockState { get; set; } = new();
}

public class ValidateSagaLockInput
{
    public SagaLockState LockState { get; set; } = new();
}

public class CreatePaymentTransactionInput
{
    public ProcessPaymentRequest Request { get; set; } = new();
    public string TransactionId { get; set; } = string.Empty;
}

public class ProcessPaymentWithGatewayInput
{
    public ProcessPaymentRequest Request { get; set; } = new();
    public string TransactionId { get; set; } = string.Empty;
}

public class UpdatePaymentStatusInput
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentGatewayResponse GatewayResponse { get; set; } = new();
}

public class CreateLedgerEntriesInput
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentGatewayResponse GatewayResponse { get; set; } = new();
}

public class ConfirmDonationInput
{
    public int DonationId { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
}

public class RefundPaymentInput
{
    public string TransactionId { get; set; } = string.Empty;
    public string GatewayTransactionId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
