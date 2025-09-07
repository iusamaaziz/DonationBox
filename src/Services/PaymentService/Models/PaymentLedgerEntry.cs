using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models;

public class PaymentLedgerEntry
{
    public int Id { get; set; }
    
    [Required]
    public int PaymentTransactionId { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    public LedgerEntryType EntryType { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Operation { get; set; } = string.Empty; // DEBIT, CREDIT, REFUND, FEE
    
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Metadata { get; set; } = string.Empty; // JSON metadata
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation properties
    public PaymentTransaction PaymentTransaction { get; set; } = null!;
}

public enum LedgerEntryType
{
    Payment = 0,
    Refund = 1,
    Fee = 2,
    Chargeback = 3,
    Adjustment = 4
}
