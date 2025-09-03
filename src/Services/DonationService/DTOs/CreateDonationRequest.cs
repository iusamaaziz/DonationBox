using System.ComponentModel.DataAnnotations;

namespace DonationService.DTOs;

public class CreateDonationRequest
{
    [Required]
    public int CampaignId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
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
}
