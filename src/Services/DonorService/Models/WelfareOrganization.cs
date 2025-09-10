using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DonorService.Models;

/// <summary>
/// Represents a welfare organization that can create donation campaigns
/// </summary>
public class WelfareOrganization
{
    /// <summary>
    /// Unique identifier for the organization
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Organization name
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of organization (Charity, Foundation, Community, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string Type { get; set; } = "Charity";

    /// <summary>
    /// Organization's mission statement
    /// </summary>
    [StringLength(500)]
    public string? Mission { get; set; }

    /// <summary>
    /// Organization's website URL
    /// </summary>
    [StringLength(500)]
    [Url]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Contact email for the organization
    /// </summary>
    [StringLength(255)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    [StringLength(20)]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Tax identification number
    /// </summary>
    [StringLength(50)]
    public string? TaxId { get; set; }

    /// <summary>
    /// User ID of the donor who created this organization
    /// </summary>
    [Required]
    [StringLength(36)]
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the organization has been verified
    /// </summary>
    public bool IsVerified { get; set; } = false;

    /// <summary>
    /// Whether the organization is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the organization was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the donor who created this organization
    /// </summary>
    [ForeignKey(nameof(CreatedByUserId))]
    public Donor? Creator { get; set; }

    /// <summary>
    /// Computed property for display purposes
    /// </summary>
    [NotMapped]
    public string DisplayName => $"{Name} ({Type})";

    /// <summary>
    /// Computed property to check if organization can create campaigns
    /// </summary>
    [NotMapped]
    public bool CanCreateCampaigns => IsActive && IsVerified;
}

/// <summary>
/// Enumeration of organization types
/// </summary>
public enum OrganizationType
{
    Charity = 0,
    Foundation = 1,
    Community = 2,
    NonProfit = 3,
    Religious = 4,
    Educational = 5,
    Health = 6,
    Environmental = 7,
    AnimalWelfare = 8,
    Other = 99
}
