using Microsoft.EntityFrameworkCore;
using DonorService.Data;
using DonorService.Models;

namespace DonorService.Services;

/// <summary>
/// Implementation of the organization service
/// </summary>
public class OrganizationServiceImpl : IOrganizationService
{
    private readonly DonorDbContext _context;
    private readonly ILogger<OrganizationServiceImpl> _logger;

    public OrganizationServiceImpl(DonorDbContext context, ILogger<OrganizationServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Create a new welfare organization
    /// </summary>
    public async Task<WelfareOrganization> CreateOrganizationAsync(
        string name,
        string? description,
        string type,
        string? mission,
        string? websiteUrl,
        string? contactEmail,
        string? contactPhone,
        string? address,
        string? taxId,
        string createdByUserId)
    {
        try
        {
            _logger.LogDebug("Creating organization: {Name} for user: {UserId}", name, createdByUserId);

            var organization = new WelfareOrganization
            {
                Name = name,
                Description = description,
                Type = type,
                Mission = mission,
                WebsiteUrl = websiteUrl,
                ContactEmail = contactEmail,
                ContactPhone = contactPhone,
                Address = address,
                TaxId = taxId,
                CreatedByUserId = createdByUserId,
                IsVerified = false, // Organizations need verification before they can create campaigns
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.WelfareOrganizations.Add(organization);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created organization: {OrganizationId} - {Name}", organization.Id, name);
            return organization;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization: {Name} for user: {UserId}", name, createdByUserId);
            throw;
        }
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    public async Task<WelfareOrganization?> GetOrganizationByIdAsync(Guid organizationId)
    {
        try
        {
            _logger.LogDebug("Getting organization by ID: {OrganizationId}", organizationId);

            var organization = await _context.WelfareOrganizations
                .Include(o => o.Creator)
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            if (organization == null)
            {
                _logger.LogDebug("Organization not found: {OrganizationId}", organizationId);
                return null;
            }

            return organization;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization by ID: {OrganizationId}", organizationId);
            throw;
        }
    }

    /// <summary>
    /// Update organization details
    /// </summary>
    public async Task<WelfareOrganization?> UpdateOrganizationAsync(
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
        string updatedByUserId)
    {
        try
        {
            _logger.LogDebug("Updating organization: {OrganizationId} by user: {UserId}", organizationId, updatedByUserId);

            var organization = await _context.WelfareOrganizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            if (organization == null)
            {
                _logger.LogWarning("Organization not found: {OrganizationId}", organizationId);
                return null;
            }

            // Check if user has permission to update
            if (organization.CreatedByUserId != updatedByUserId)
            {
                _logger.LogWarning("User {UserId} attempted to update organization {OrganizationId} without permission",
                    updatedByUserId, organizationId);
                throw new UnauthorizedAccessException("You don't have permission to update this organization");
            }

            // Update fields if provided
            if (!string.IsNullOrEmpty(name)) organization.Name = name;
            if (description != null) organization.Description = description;
            if (!string.IsNullOrEmpty(type)) organization.Type = type;
            if (mission != null) organization.Mission = mission;
            if (websiteUrl != null) organization.WebsiteUrl = websiteUrl;
            if (contactEmail != null) organization.ContactEmail = contactEmail;
            if (contactPhone != null) organization.ContactPhone = contactPhone;
            if (address != null) organization.Address = address;
            if (taxId != null) organization.TaxId = taxId;

            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated organization: {OrganizationId}", organizationId);
            return organization;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization: {OrganizationId}", organizationId);
            throw;
        }
    }

    /// <summary>
    /// Get all organizations with pagination
    /// </summary>
    public async Task<(IEnumerable<WelfareOrganization>, int)> GetAllOrganizationsAsync(int page, int pageSize, string? filterType)
    {
        try
        {
            _logger.LogDebug("Getting organizations - Page: {Page}, Size: {Size}, Filter: {Filter}",
                page, pageSize, filterType);

            var query = _context.WelfareOrganizations.Where(o => o.IsActive);

            if (!string.IsNullOrEmpty(filterType))
            {
                query = query.Where(o => o.Type == filterType);
            }

            var totalCount = await query.CountAsync();

            var organizations = await query
                .Include(o => o.Creator)
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            _logger.LogDebug("Found {Count} organizations (total: {Total})", organizations.Count, totalCount);
            return (organizations, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations");
            throw;
        }
    }

    /// <summary>
    /// Validate if user has access to manage organization
    /// </summary>
    public async Task<bool> ValidateOrganizationAccessAsync(Guid organizationId, string userId)
    {
        try
        {
            _logger.LogDebug("Validating access for user {UserId} to organization {OrganizationId}",
                userId, organizationId);

            var organization = await _context.WelfareOrganizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            if (organization == null)
            {
                _logger.LogWarning("Organization not found: {OrganizationId}", organizationId);
                return false;
            }

            var hasAccess = organization.CreatedByUserId == userId;
            _logger.LogDebug("Access validation result: {HasAccess}", hasAccess);

            return hasAccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating organization access for user {UserId} to organization {OrganizationId}",
                userId, organizationId);
            throw;
        }
    }

    /// <summary>
    /// Delete organization
    /// </summary>
    public async Task<bool> DeleteOrganizationAsync(Guid organizationId, string userId)
    {
        try
        {
            _logger.LogDebug("Deleting organization: {OrganizationId} by user: {UserId}", organizationId, userId);

            var organization = await _context.WelfareOrganizations
                .FirstOrDefaultAsync(o => o.Id == organizationId && o.IsActive);

            if (organization == null)
            {
                _logger.LogWarning("Organization not found: {OrganizationId}", organizationId);
                return false;
            }

            if (organization.CreatedByUserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to delete organization {OrganizationId} without permission",
                    userId, organizationId);
                throw new UnauthorizedAccessException("You don't have permission to delete this organization");
            }

            // Soft delete by marking as inactive
            organization.IsActive = false;
            organization.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted organization: {OrganizationId}", organizationId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organization: {OrganizationId}", organizationId);
            throw;
        }
    }
}
