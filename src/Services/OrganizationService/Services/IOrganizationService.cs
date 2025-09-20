using OrganizationService.DTOs;

namespace OrganizationService.Services;

/// <summary>
/// Interface for organization service operations
/// </summary>
public interface IOrganizationService
{
    /// <summary>
    /// Creates a new organization
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <param name="userId">ID of the user creating the organization</param>
    /// <returns>Created organization response</returns>
    Task<OrganizationResponse?> CreateOrganizationAsync(CreateOrganizationRequest request, string userId);

    /// <summary>
    /// Gets all active organizations
    /// </summary>
    /// <returns>List of organization summaries</returns>
    Task<List<OrganizationSummary>> GetOrganizationsAsync();

    /// <summary>
    /// Gets organizations created by a specific user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>List of organization summaries created by the user</returns>
    Task<List<OrganizationSummary>> GetOrganizationsByUserAsync(string userId);

    /// <summary>
    /// Gets an organization by ID
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Organization response if found</returns>
    Task<OrganizationResponse?> GetOrganizationAsync(Guid organizationId);

    /// <summary>
    /// Updates an organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">ID of the user making the update</param>
    /// <returns>Updated organization response if successful</returns>
    Task<OrganizationResponse?> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request, string userId);

    /// <summary>
    /// Soft deletes an organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">ID of the user making the deletion</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteOrganizationAsync(Guid organizationId, string userId);
}

