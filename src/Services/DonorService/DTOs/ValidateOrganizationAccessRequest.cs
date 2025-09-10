using System.ComponentModel.DataAnnotations;

namespace DonorService.DTOs;

/// <summary>
/// Request DTO for validating organization access
/// </summary>
public class ValidateOrganizationAccessRequest
{
    /// <summary>
    /// Organization ID to validate access for
    /// </summary>
    [Required]
    public string OrganizationId { get; set; } = string.Empty;

    /// <summary>
    /// User ID requesting access
    /// </summary>
    [Required]
    public string UserId { get; set; } = string.Empty;
}
