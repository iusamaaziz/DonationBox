namespace OrganizationService.DTOs;

/// <summary>
/// Response DTO for organization data
/// </summary>
public class OrganizationResponse
{
    /// <summary>
    /// Unique identifier for the organization
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the organization
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the organization and its mission
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Physical address of the organization
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Phone number for the organization
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Email address for the organization
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Website URL for the organization
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// ID of the user who created this organization
    /// </summary>
    public Guid CreatedBy { get; set; }

    /// <summary>
    /// Timestamp when the organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Timestamp when the organization was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Indicates whether the organization is active
    /// </summary>
    public bool IsActive { get; set; }
}

