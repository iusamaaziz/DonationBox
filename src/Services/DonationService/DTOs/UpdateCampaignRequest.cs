using System.ComponentModel.DataAnnotations;
using DonationService.Models;

namespace DonationService.DTOs;

public class UpdateCampaignRequest
{
    [MaxLength(200)]
    public string? Title { get; set; }
    
    [MaxLength(1000)]
    public string? Description { get; set; }
    
    [Range(1, double.MaxValue, ErrorMessage = "Goal must be greater than 0")]
    public decimal? Goal { get; set; }
    
    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }
    
    public CampaignStatus? Status { get; set; }
}
