using CampaignService.Models;

namespace CampaignService.DTOs;

public class UpdateCampaignRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public decimal? Goal { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public CampaignStatus? Status { get; set; }
}
