namespace OrganizationService.DTOs;

/// <summary>
/// Summary DTO for organization listing
/// </summary>
public class OrganizationSummary
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
    /// Email address for the organization
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Website URL for the organization
    /// </summary>
    public string? Website { get; set; }

    /// <summary>
    /// Timestamp when the organization was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

