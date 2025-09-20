using Microsoft.EntityFrameworkCore;

using OrganizationService.Data;
using OrganizationService.DTOs;
using OrganizationService.Models;

namespace OrganizationService.Services;

/// <summary>
/// Service for managing organization operations
/// </summary>
public class OrganizationService : IOrganizationService
{
    private readonly OrganizationDbContext _context;
    private readonly IAuthValidationService _authClient;
    private readonly ILogger<OrganizationService> _logger;

    public OrganizationService(
        OrganizationDbContext context,
        IAuthValidationService authClient,
        ILogger<OrganizationService> logger)
    {
        _context = context;
        _authClient = authClient;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new organization
    /// </summary>
    /// <param name="request">Organization creation request</param>
    /// <param name="userId">ID of the user creating the organization</param>
    /// <returns>Created organization response</returns>
    public async Task<OrganizationResponse?> CreateOrganizationAsync(CreateOrganizationRequest request, string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return null;
            }

            var organization = new Organization
            {
                Name = request.Name,
                Description = request.Description,
                Address = request.Address,
                Phone = request.Phone,
                Email = request.Email,
                Website = request.Website,
                CreatedBy = userGuid,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();

            return MapToResponse(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization");
            return null;
        }
    }

    /// <summary>
    /// Gets all active organizations
    /// </summary>
    /// <returns>List of organization summaries</returns>
    public async Task<List<OrganizationSummary>> GetOrganizationsAsync()
    {
        try
        {
            var organizations = await _context.Organizations
                .Where(o => o.IsActive)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrganizationSummary
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description,
                    Email = o.Email,
                    Website = o.Website,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return organizations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations");
            return new List<OrganizationSummary>();
        }
    }

    /// <summary>
    /// Gets organizations created by a specific user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <returns>List of organization summaries created by the user</returns>
    public async Task<List<OrganizationSummary>> GetOrganizationsByUserAsync(string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return new List<OrganizationSummary>();
            }

            var organizations = await _context.Organizations
                .Where(o => o.IsActive && o.CreatedBy == userGuid)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new OrganizationSummary
                {
                    Id = o.Id,
                    Name = o.Name,
                    Description = o.Description,
                    Email = o.Email,
                    Website = o.Website,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return organizations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations for user {UserId}", userId);
            return new List<OrganizationSummary>();
        }
    }

    /// <summary>
    /// Gets an organization by ID
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <returns>Organization response if found</returns>
    public async Task<OrganizationResponse?> GetOrganizationAsync(Guid organizationId)
    {
        try
        {
            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            return organization != null ? MapToResponse(organization) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization {OrganizationId}", organizationId);
            return null;
        }
    }

    /// <summary>
    /// Updates an organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="request">Update request</param>
    /// <param name="userId">ID of the user making the update</param>
    /// <returns>Updated organization response if successful</returns>
    public async Task<OrganizationResponse?> UpdateOrganizationAsync(Guid organizationId, UpdateOrganizationRequest request, string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return null;
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            if (organization == null)
            {
                return null;
            }

            // Check if user is the creator
            if (organization.CreatedBy != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to update organization {OrganizationId} they didn't create", userId, organizationId);
                return null;
            }

            // Update fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
                organization.Name = request.Name;

            if (request.Description != null)
                organization.Description = request.Description;

            if (request.Address != null)
                organization.Address = request.Address;

            if (request.Phone != null)
                organization.Phone = request.Phone;

            if (request.Email != null)
                organization.Email = request.Email;

            if (request.Website != null)
                organization.Website = request.Website;

            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToResponse(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization {OrganizationId}", organizationId);
            return null;
        }
    }

    /// <summary>
    /// Soft deletes an organization
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="userId">ID of the user making the deletion</param>
    /// <returns>True if successful</returns>
    public async Task<bool> DeleteOrganizationAsync(Guid organizationId, string userId)
    {
        try
        {
            if (!Guid.TryParse(userId, out var userGuid))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                return false;
            }

            var organization = await _context.Organizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            if (organization == null)
            {
                return false;
            }

            // Check if user is the creator
            if (organization.CreatedBy != userGuid)
            {
                _logger.LogWarning("User {UserId} attempted to delete organization {OrganizationId} they didn't create", userId, organizationId);
                return false;
            }

            organization.IsActive = false;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organization {OrganizationId}", organizationId);
            return false;
        }
    }

    private static OrganizationResponse MapToResponse(Organization organization)
    {
        return new OrganizationResponse
        {
            Id = organization.Id,
            Name = organization.Name,
            Description = organization.Description,
            Address = organization.Address,
            Phone = organization.Phone,
            Email = organization.Email,
            Website = organization.Website,
            CreatedBy = organization.CreatedBy,
            CreatedAt = organization.CreatedAt,
            UpdatedAt = organization.UpdatedAt,
            IsActive = organization.IsActive
        };
    }
}

