using System.ComponentModel.DataAnnotations;

namespace CampaignService.Models;

public class DonationCampaign
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public decimal Goal { get; set; }

    public decimal CurrentAmount { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public CampaignStatus Status { get; set; }

    [Required]
    [MaxLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Calculated properties
    public decimal ProgressPercentage => Goal > 0 ? Math.Round((CurrentAmount / Goal) * 100, 2) : 0;

    public bool IsActive => Status == CampaignStatus.Active && DateTime.UtcNow >= StartDate && DateTime.UtcNow <= EndDate;

    public bool IsExpired => DateTime.UtcNow > EndDate;

    public bool IsGoalReached => CurrentAmount >= Goal;

    public TimeSpan TimeRemaining => EndDate > DateTime.UtcNow ? EndDate - DateTime.UtcNow : TimeSpan.Zero;
}

public enum CampaignStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Completed = 3,
    Cancelled = 4
}
