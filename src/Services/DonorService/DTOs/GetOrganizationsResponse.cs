namespace DonorService.DTOs;

/// <summary>
/// Response DTO for paginated organization list
/// </summary>
public class GetOrganizationsResponse
{
    /// <summary>
    /// List of organizations
    /// </summary>
    public IEnumerable<OrganizationDto> Organizations { get; set; } = new List<OrganizationDto>();

    /// <summary>
    /// Total count of organizations
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Error message if any
    /// </summary>
    public string? ErrorMessage { get; set; }
}
