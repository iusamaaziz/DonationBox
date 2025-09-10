namespace DonorService.DTOs;

/// <summary>
/// Request DTO for getting organizations with pagination
/// </summary>
public class GetOrganizationsRequest
{
    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Optional filter by organization type
    /// </summary>
    public string? FilterType { get; set; }
}
