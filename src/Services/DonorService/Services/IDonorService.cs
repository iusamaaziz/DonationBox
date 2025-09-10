using DonorService.Models;

namespace DonorService.Services;

/// <summary>
/// Interface for donor service operations
/// </summary>
public interface IDonorService
{
    /// <summary>
    /// Get donor by user ID
    /// </summary>
    Task<Donor?> GetDonorByUserIdAsync(string userId);

    /// <summary>
    /// Create or update donor profile
    /// </summary>
    Task<Donor> CreateOrUpdateDonorAsync(string userId, string? bio, List<string> interests, string? phoneNumber, string? address);

    /// <summary>
    /// Get organizations created by a donor
    /// </summary>
    Task<IEnumerable<WelfareOrganization>> GetDonorOrganizationsAsync(string userId);
}
