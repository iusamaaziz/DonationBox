using System.ComponentModel.DataAnnotations;

namespace DonorService.DTOs;

/// <summary>
/// Request DTO for updating organization details
/// </summary>
public class UpdateOrganizationRequest
{
    /// <summary>
    /// Organization name
    /// </summary>
    [StringLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// Organization description
    /// </summary>
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Type of organization (Charity, Foundation, Community, etc.)
    /// </summary>
    [StringLength(50)]
    public string? Type { get; set; }

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
    /// User ID of the user updating the organization
    /// </summary>
    [Required]
    public string UpdatedByUserId { get; set; } = string.Empty;
}
