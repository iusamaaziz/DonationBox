using DonationService.Models;

namespace DonationService.DTOs;

public class CampaignResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Goal { get; set; }
    public decimal CurrentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public CampaignStatus Status { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public decimal ProgressPercentage { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsGoalReached { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public int TotalDonations { get; set; }
}

public class CampaignStatsResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Goal { get; set; }
    public decimal CurrentAmount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public int TotalDonations { get; set; }
    public bool IsGoalReached { get; set; }
    public TimeSpan TimeRemaining { get; set; }
    public DateTime LastUpdated { get; set; }
}
