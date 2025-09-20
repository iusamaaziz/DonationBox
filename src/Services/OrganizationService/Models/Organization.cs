using System.ComponentModel.DataAnnotations;

namespace OrganizationService.Models;

/// <summary>
/// Represents a charity organization in the system
/// </summary>
public class Organization
{
    /// <summary>
    /// Unique identifier for the organization
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the organization
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the organization and its mission
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Physical address of the organization
    /// </summary>
    [StringLength(500)]
    public string? Address { get; set; }

    /// <summary>
    /// Phone number for the organization
    /// </summary>
    [StringLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Email address for the organization
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Website URL for the organization
    /// </summary>
    [Url]
    [StringLength(500)]
    public string? Website { get; set; }

    /// <summary>
    /// ID of the user who created this organization
    /// </summary>
    [Required]
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when the organization was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Indicates whether the organization is active (not soft-deleted)
    /// </summary>
    public bool IsActive { get; set; } = true;
}

