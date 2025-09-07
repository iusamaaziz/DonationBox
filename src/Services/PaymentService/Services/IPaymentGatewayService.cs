using PaymentService.DTOs;
using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentGatewayService
{
    Task<PaymentGatewayResponse> ProcessPaymentAsync(ProcessPaymentRequest request, string transactionId);
    Task<RefundGatewayResponse> RefundPaymentAsync(string gatewayTransactionId, decimal amount, string reason);
    Task<PaymentGatewayStatus> GetPaymentStatusAsync(string gatewayTransactionId);
}

public class PaymentGatewayResponse
{
    public bool IsSuccess { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public decimal ProcessingFee { get; set; }
    public DateTime ProcessedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class RefundGatewayResponse
{
    public bool IsSuccess { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public decimal RefundedAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class PaymentGatewayStatus
{
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
