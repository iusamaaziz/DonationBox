using System.ComponentModel.DataAnnotations;

namespace DonorService.DTOs;

/// <summary>
/// DTO for organization information
/// </summary>
public class OrganizationDto
{
    /// <summary>
    /// Unique identifier for the organization
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Organization name
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Organization description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of organization (Charity, Foundation, Community, etc.)
    /// </summary>
    [Required]
    public string Type { get; set; } = "Charity";

    /// <summary>
    /// Organization's mission statement
    /// </summary>
    public string? Mission { get; set; }

    /// <summary>
    /// Organization's website URL
    /// </summary>
    [Url]
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Contact email for the organization
    /// </summary>
    [EmailAddress]
    public string? ContactEmail { get; set; }

    /// <summary>
    /// Contact phone number
    /// </summary>
    public string? ContactPhone { get; set; }

    /// <summary>
    /// Physical address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Tax identification number
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// User ID of the donor who created this organization
    /// </summary>
    public string CreatedByUserId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the organization has been verified
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Whether the organization is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the organization was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Computed property for display purposes
    /// </summary>
    public string DisplayName => $"{Name} ({Type})";

    /// <summary>
    /// Computed property to check if organization can create campaigns
    /// </summary>
    public bool CanCreateCampaigns => IsActive && IsVerified;
}
