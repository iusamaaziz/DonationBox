namespace DonorService.DTOs;

/// <summary>
/// Response DTO for organization access validation
/// </summary>
public class ValidateOrganizationAccessResponse
{
    /// <summary>
    /// Whether the user has access to the organization
    /// </summary>
    public bool HasAccess { get; set; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
