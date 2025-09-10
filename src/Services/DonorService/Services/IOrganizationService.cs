using DonorService.Models;

namespace DonorService.Services;

/// <summary>
/// Interface for organization service operations
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Create a new welfare organization
    /// </summary>
    Task<WelfareOrganization> CreateOrganizationAsync(
        string name,
        string? description,
        string type,
        string? mission,
        string? websiteUrl,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? taxId,
        string createdByUserId);

    /// <summary>
    /// Get organization by ID
    /// </summary>
    Task<WelfareOrganization?> GetOrganizationByIdAsync(Guid organizationId);

    /// <summary>
    /// Update organization details
    /// </summary>
    Task<WelfareOrganization?> UpdateOrganizationAsync(
        Guid organizationId,
        string? name,
        string? description,
        string? type,
        string? mission,
        string? websiteUrl,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? taxId,
        string updatedByUserId);

    /// <summary>
    /// Get all organizations with pagination
    /// </summary>
    Task<(IEnumerable<WelfareOrganization>, int)> GetAllOrganizationsAsync(int page, int pageSize, string? filterType);

    /// <summary>
    /// Validate if user has access to manage organization
    /// </summary>
    Task<bool> ValidateOrganizationAccessAsync(Guid organizationId, string userId);

    /// <summary>
    /// Delete organization
    /// </summary>
    Task<bool> DeleteOrganizationAsync(Guid organizationId, string userId);
}
