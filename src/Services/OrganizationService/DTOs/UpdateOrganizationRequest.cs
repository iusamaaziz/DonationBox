using System.ComponentModel.DataAnnotations;

namespace OrganizationService.DTOs;

/// <summary>
/// Request DTO for updating an existing organization
/// </summary>
public class UpdateOrganizationRequest
{
    /// <summary>
    /// Name of the organization
    /// </summary>
    [StringLength(200, MinimumLength = 1)]
    public string? Name { get; set; }

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
}

