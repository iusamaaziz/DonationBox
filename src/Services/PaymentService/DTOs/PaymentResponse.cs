using System.ComponentModel.DataAnnotations;
using PaymentService.Models;

namespace PaymentService.DTOs;

public class PaymentResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public int DonationId { get; set; }
    public int CampaignId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentGateway { get; set; } = string.Empty;
    public string GatewayTransactionId { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class RefundRequest
{
    [Required]
    public string TransactionId { get; set; } = string.Empty;
    
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal? Amount { get; set; } // Null for full refund
    
    [MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class RefundResponse
{
    public string RefundId { get; set; } = string.Empty;
    public string OriginalTransactionId { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentStatusResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<LedgerEntryResponse> LedgerEntries { get; set; } = new();
}

public class LedgerEntryResponse
{
    public decimal Amount { get; set; }
    public LedgerEntryType EntryType { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
