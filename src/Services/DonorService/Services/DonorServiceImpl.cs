using Microsoft.EntityFrameworkCore;
using DonorService.Data;
using DonorService.Models;

namespace DonorService.Services;

/// <summary>
/// Implementation of the donor service
/// </summary>
public class DonorServiceImpl : IDonorService
{
    private readonly DonorDbContext _context;
    private readonly ILogger<DonorServiceImpl> _logger;

    public DonorServiceImpl(DonorDbContext context, ILogger<DonorServiceImpl> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get donor by user ID
    /// </summary>
    public async Task<Donor?> GetDonorByUserIdAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting donor by user ID: {UserId}", userId);

            var donor = await _context.Donors
                .Include(d => d.Organizations)
                .FirstOrDefaultAsync(d => d.UserId == userId);

            if (donor == null)
            {
                _logger.LogDebug("Donor not found for user ID: {UserId}", userId);
                return null;
            }

            _logger.LogDebug("Found donor: {UserId} with {OrganizationCount} organizations",
                userId, donor.Organizations.Count);
            return donor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting donor by user ID: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Create or update donor profile
    /// </summary>
    public async Task<Donor> CreateOrUpdateDonorAsync(
        string userId,
        string? bio,
        List<string> interests,
        string? phoneNumber,
        string? address)
    {
        try
        {
            _logger.LogDebug("Creating or updating donor profile for user: {UserId}", userId);

            var existingDonor = await _context.Donors.FirstOrDefaultAsync(d => d.UserId == userId);

            if (existingDonor != null)
            {
                // Update existing donor
                existingDonor.Bio = bio;
                existingDonor.InterestsList = interests;
                existingDonor.PhoneNumber = phoneNumber;
                existingDonor.Address = address;
                existingDonor.UpdatedAt = DateTime.UtcNow;

                _logger.LogDebug("Updated existing donor profile: {UserId}", userId);
            }
            else
            {
                // Create new donor
                var newDonor = new Donor
                {
                    UserId = userId,
                    Bio = bio,
                    InterestsList = interests,
                    PhoneNumber = phoneNumber,
                    Address = address,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Donors.Add(newDonor);
                existingDonor = newDonor;

                _logger.LogDebug("Created new donor profile: {UserId}", userId);
            }

            await _context.SaveChangesAsync();
            return existingDonor;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating donor profile for user: {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Get organizations created by a donor
    /// </summary>
    public async Task<IEnumerable<WelfareOrganization>> GetDonorOrganizationsAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting organizations for donor: {UserId}", userId);

            var organizations = await _context.WelfareOrganizations
                .Where(o => o.CreatedByUserId == userId && o.IsActive)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            _logger.LogDebug("Found {Count} organizations for donor: {UserId}",
                organizations.Count, userId);

            return organizations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organizations for donor: {UserId}", userId);
            throw;
        }
    }
}
