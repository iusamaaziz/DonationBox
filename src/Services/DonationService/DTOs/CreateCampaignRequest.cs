using System.ComponentModel.DataAnnotations;

namespace DonationService.DTOs;

public class CreateCampaignRequest
{
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [Range(1, double.MaxValue, ErrorMessage = "Goal must be greater than 0")]
    public decimal Goal { get; set; }
    
    [Required]
    public DateTime StartDate { get; set; }
    
    [Required]
    public DateTime EndDate { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;
}
