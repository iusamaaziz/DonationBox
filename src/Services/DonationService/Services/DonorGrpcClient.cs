using Grpc.Net.Client;
using DonorService.Grpc;

namespace DonationService.Services;

/// <summary>
/// gRPC client for communicating with DonorService
/// </summary>
public class DonorClient : IDisposable
{
    private readonly GrpcChannel _channel;
    private readonly DonorService.Grpc.DonorService.DonorServiceClient _client;
    private readonly ILogger<DonorClient> _logger;

    public DonorClient(string donorServiceUrl, ILogger<DonorClient> logger)
    {
        _logger = logger;
        _channel = GrpcChannel.ForAddress(donorServiceUrl);
        _client = new DonorService.Grpc.DonorService.DonorServiceClient(_channel);
    }

    /// <summary>
    /// Get donor profile by user ID
    /// </summary>
    public async Task<(bool Found, Donor? Donor, string? ErrorMessage)> GetDonorAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting donor via DonorService gRPC: {UserId}", userId);

            var request = new GetDonorRequest { UserId = userId };
            var response = await _client.GetDonorAsync(request);

            if (!response.Found)
            {
                _logger.LogDebug("Donor not found via DonorService: {UserId}", userId);
                return (false, null, response.ErrorMessage);
            }

            return (true, response.Donor, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting donor via DonorService gRPC: {UserId}", userId);
            return (false, null, "DonorService communication error");
        }
    }

    /// <summary>
    /// Get organization details by ID
    /// </summary>
    public async Task<(bool Found, Organization? Organization, string? ErrorMessage)> GetOrganizationAsync(string organizationId)
    {
        try
        {
            _logger.LogDebug("Getting organization via DonorService gRPC: {OrganizationId}", organizationId);

            var request = new GetOrganizationRequest { OrganizationId = organizationId };
            var response = await _client.GetOrganizationAsync(request);

            if (!response.Found)
            {
                _logger.LogDebug("Organization not found via DonorService: {OrganizationId}", organizationId);
                return (false, null, response.ErrorMessage);
            }

            return (true, response.Organization, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting organization via DonorService gRPC: {OrganizationId}", organizationId);
            return (false, null, "DonorService communication error");
        }
    }

    /// <summary>
    /// Get all organizations
    /// </summary>
    public async Task<(IEnumerable<Organization> Organizations, int TotalCount, string? ErrorMessage)> GetAllOrganizationsAsync(int page = 1, int pageSize = 20, string? filterType = null)
    {
        try
        {
            _logger.LogDebug("Getting all organizations via DonorService gRPC - Page: {Page}, Size: {Size}", page, pageSize);

            var request = new GetAllOrganizationsRequest
            {
                Page = page,
                PageSize = pageSize,
                FilterType = filterType ?? string.Empty
            };

            var response = await _client.GetAllOrganizationsAsync(request);

            return (response.Organizations, response.TotalCount, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all organizations via DonorService gRPC");
            return (new List<Organization>(), 0, "DonorService communication error");
        }
    }

    /// <summary>
    /// Validate organization access for a user
    /// </summary>
    public async Task<(bool HasAccess, string? ErrorMessage)> ValidateOrganizationAccessAsync(string organizationId, string userId)
    {
        try
        {
            _logger.LogDebug("Validating organization access via DonorService gRPC - Org: {OrganizationId}, User: {UserId}",
                organizationId, userId);

            var request = new ValidateOrganizationAccessRequest
            {
                OrganizationId = organizationId,
                UserId = userId
            };

            var response = await _client.ValidateOrganizationAccessAsync(request);

            return (response.HasAccess, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating organization access via DonorService gRPC");
            return (false, "DonorService communication error");
        }
    }

    /// <summary>
    /// Get donor's organizations
    /// </summary>
    public async Task<(IEnumerable<Organization> Organizations, string? ErrorMessage)> GetDonorOrganizationsAsync(string userId)
    {
        try
        {
            _logger.LogDebug("Getting donor organizations via DonorService gRPC: {UserId}", userId);

            var request = new GetDonorOrganizationsRequest { UserId = userId };
            var response = await _client.GetDonorOrganizationsAsync(request);

            return (response.Organizations, response.ErrorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting donor organizations via DonorService gRPC: {UserId}", userId);
            return (new List<Organization>(), "DonorService communication error");
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
    }
}
