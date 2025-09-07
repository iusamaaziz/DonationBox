using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models;

public class PaymentTransaction
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    public int DonationId { get; set; }
    
    [Required]
    public int CampaignId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = "USD";
    
    [Required]
    [MaxLength(100)]
    public string DonorName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string DonorEmail { get; set; } = string.Empty;
    
    public PaymentStatus Status { get; set; }
    
    public PaymentMethod PaymentMethod { get; set; }
    
    [MaxLength(100)]
    public string PaymentGateway { get; set; } = string.Empty;
    
    [MaxLength(100)]
    public string GatewayTransactionId { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string FailureReason { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public ICollection<PaymentLedgerEntry> LedgerEntries { get; set; } = new List<PaymentLedgerEntry>();
    
    public ICollection<OutboxEvent> OutboxEvents { get; set; } = new List<OutboxEvent>();
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4,
    PartiallyRefunded = 5,
    Cancelled = 6
}

public enum PaymentMethod
{
    CreditCard = 0,
    DebitCard = 1,
    PayPal = 2,
    BankTransfer = 3,
    ApplePay = 4,
    GooglePay = 5,
    Other = 99
}
