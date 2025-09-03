using System.ComponentModel.DataAnnotations;

namespace DonationService.Models;

public class Donation
{
    public int Id { get; set; }
    
    [Required]
    public int CampaignId { get; set; }
    
    [Required]
    public decimal Amount { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string DonorName { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string DonorEmail { get; set; } = string.Empty;
    
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    
    public bool IsAnonymous { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string TransactionId { get; set; } = string.Empty;
    
    public PaymentStatus PaymentStatus { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? ProcessedAt { get; set; }
    
    // Navigation properties
    public DonationCampaign Campaign { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    Refunded = 4
}
