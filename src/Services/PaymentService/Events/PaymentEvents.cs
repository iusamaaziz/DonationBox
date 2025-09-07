using PaymentService.Models;

namespace PaymentService.Events;

public class PaymentProcessedEvent
{
    public string TransactionId { get; set; } = string.Empty;
    public int DonationId { get; set; }
    public int CampaignId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}

public class PaymentCompletedEvent
{
    public string TransactionId { get; set; } = string.Empty;
    public int DonationId { get; set; }
    public int CampaignId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public string GatewayTransactionId { get; set; } = string.Empty;
}

public class PaymentFailedEvent
{
    public string TransactionId { get; set; } = string.Empty;
    public int DonationId { get; set; }
    public int CampaignId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string FailureReason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}

public class PaymentRefundedEvent
{
    public string RefundId { get; set; } = string.Empty;
    public string OriginalTransactionId { get; set; } = string.Empty;
    public int DonationId { get; set; }
    public int CampaignId { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime RefundedAt { get; set; }
}
