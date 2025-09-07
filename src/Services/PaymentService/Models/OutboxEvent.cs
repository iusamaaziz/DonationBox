using System.ComponentModel.DataAnnotations;

namespace PaymentService.Models;

public class OutboxEvent
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string EventId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string EventType { get; set; } = string.Empty;
    
    [Required]
    public string EventData { get; set; } = string.Empty; // JSON payload
    
    public int? PaymentTransactionId { get; set; }
    
    public OutboxEventStatus Status { get; set; }
    
    public int RetryCount { get; set; }
    
    public DateTime? NextRetryAt { get; set; }
    
    [MaxLength(1000)]
    public string ErrorMessage { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public PaymentTransaction? PaymentTransaction { get; set; }
}

public enum OutboxEventStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
