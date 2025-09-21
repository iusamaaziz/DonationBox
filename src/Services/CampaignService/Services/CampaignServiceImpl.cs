using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using CampaignService.Data;
using CampaignService.DTOs;
using CampaignService.Models;

namespace CampaignService.Services;

public class CampaignServiceImpl : ICampaignService
{
    private readonly CampaignDbContext _context;
    private readonly IDistributedCache? _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CampaignServiceImpl> _logger;
    private readonly bool _useRedis;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public CampaignServiceImpl(
        CampaignDbContext context,
        IConfiguration configuration,
        ILogger<CampaignServiceImpl> logger,
        IDistributedCache? cache = null)
    {
        _context = context;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _useRedis = _configuration.GetValue<bool>("UseRedis");
    }

    public async Task<IEnumerable<CampaignResponse>> GetAllCampaignsAsync()
    {
        var campaigns = await _context.Campaigns
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapToResponse);
    }

    public async Task<IEnumerable<CampaignResponse>> GetActiveCampaignsAsync()
    {
        const string cacheKey = "active_campaigns";

        if (_useRedis && _cache != null)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Retrieved active campaigns from cache");
                    var cachedCampaigns = JsonSerializer.Deserialize<IEnumerable<CampaignResponse>>(cachedData);
                    if (cachedCampaigns != null)
                        return cachedCampaigns;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve active campaigns from cache, falling back to database");
            }
        }

        var activeCampaigns = await _context.Campaigns
            .Where(c => c.Status == CampaignStatus.Active &&
                       DateTime.UtcNow >= c.StartDate &&
                       DateTime.UtcNow <= c.EndDate)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        var result = activeCampaigns.Select(MapToResponse).ToList();

        if (_useRedis && _cache != null && result.Any())
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(result, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = _cacheExpiration
                });
                _logger.LogInformation("Cached active campaigns for {Duration} minutes", _cacheExpiration.TotalMinutes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache active campaigns, continuing without cache");
            }
        }

        return result;
    }

    public async Task<CampaignResponse?> GetCampaignByIdAsync(int id)
    {
        var campaign = await _context.Campaigns.FindAsync(id);
        return campaign != null ? MapToResponse(campaign) : null;
    }

    public async Task<CampaignStatsResponse?> GetCampaignStatsAsync(int id)
    {
        var cacheKey = $"campaign_stats_{id}";

        if (_useRedis && _cache != null)
        {
            try
            {
                var cachedData = await _cache.GetStringAsync(cacheKey);
                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation("Retrieved campaign {CampaignId} stats from cache", id);
                    var cachedStats = JsonSerializer.Deserialize<CampaignStatsResponse>(cachedData);
                    if (cachedStats != null)
                        return cachedStats;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve campaign {CampaignId} stats from cache, falling back to database", id);
            }
        }

        var campaign = await _context.Campaigns.FindAsync(id);

        if (campaign == null)
            return null;

        var stats = new CampaignStatsResponse
        {
            Id = campaign.Id,
            Title = campaign.Title,
            Goal = campaign.Goal,
            CurrentAmount = campaign.CurrentAmount,
            ProgressPercentage = campaign.ProgressPercentage,
            TotalDonations = 0, // We'll calculate this from donation events
            IsGoalReached = campaign.IsGoalReached,
            TimeRemaining = campaign.TimeRemaining,
            LastUpdated = DateTime.UtcNow
        };

        if (_useRedis && _cache != null)
        {
            try
            {
                var serializedData = JsonSerializer.Serialize(stats, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await _cache.SetStringAsync(cacheKey, serializedData, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) // Shorter cache for stats
                });
                _logger.LogInformation("Cached campaign {CampaignId} stats for 5 minutes", id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cache campaign {CampaignId} stats, continuing without cache", id);
            }
        }

        return stats;
    }

    public async Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request)
    {
        var campaign = new DonationCampaign
        {
            Title = request.Title,
            Description = request.Description,
            Goal = request.Goal,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            CreatedBy = request.CreatedBy,
            Status = CampaignStatus.Draft,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Campaigns.Add(campaign);
        await _context.SaveChangesAsync();

        // Invalidate active campaigns cache
        await InvalidateActiveCampaignsCache();

        _logger.LogInformation("Created new campaign with ID {CampaignId}", campaign.Id);
        return MapToResponse(campaign);
    }

    public async Task<CampaignResponse?> UpdateCampaignAsync(int id, UpdateCampaignRequest request)
    {
        var campaign = await _context.Campaigns.FindAsync(id);
        if (campaign == null)
            return null;

        // Update only provided fields
        if (!string.IsNullOrEmpty(request.Title))
            campaign.Title = request.Title;

        if (!string.IsNullOrEmpty(request.Description))
            campaign.Description = request.Description;

        if (request.Goal.HasValue)
            campaign.Goal = request.Goal.Value;

        if (request.StartDate.HasValue)
            campaign.StartDate = request.StartDate.Value;

        if (request.EndDate.HasValue)
            campaign.EndDate = request.EndDate.Value;

        if (request.Status.HasValue)
            campaign.Status = request.Status.Value;

        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate related caches
        await InvalidateActiveCampaignsCache();
        await InvalidateCampaignStatsCache(id);

        _logger.LogInformation("Updated campaign with ID {CampaignId}", id);
        return MapToResponse(campaign);
    }

    public async Task<bool> DeleteCampaignAsync(int id)
    {
        var campaign = await _context.Campaigns.FindAsync(id);
        if (campaign == null)
            return false;

        _context.Campaigns.Remove(campaign);
        await _context.SaveChangesAsync();

        // Invalidate related caches
        await InvalidateActiveCampaignsCache();
        await InvalidateCampaignStatsCache(id);

        _logger.LogInformation("Deleted campaign with ID {CampaignId}", id);
        return true;
    }

    public async Task<bool> UpdateCampaignStatusAsync(int id, CampaignStatus status)
    {
        var campaign = await _context.Campaigns.FindAsync(id);
        if (campaign == null)
            return false;

        campaign.Status = status;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate related caches
        await InvalidateActiveCampaignsCache();
        await InvalidateCampaignStatsCache(id);

        _logger.LogInformation("Updated campaign {CampaignId} status to {Status}", id, status);
        return true;
    }

    public async Task RefreshCampaignStatsAsync(int campaignId)
    {
        // This method would be called when we receive donation events
        // For now, we'll just invalidate the cache
        await InvalidateActiveCampaignsCache();
        await InvalidateCampaignStatsCache(campaignId);

        _logger.LogInformation("Refreshed stats for campaign {CampaignId}", campaignId);
    }

    public async Task<IEnumerable<CampaignResponse>> GetCampaignsByCreatorAsync(string createdBy)
    {
        var campaigns = await _context.Campaigns
            .Where(c => c.CreatedBy == createdBy)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return campaigns.Select(MapToResponse);
    }

    public async Task UpdateCampaignAmountAsync(int campaignId, decimal amount)
    {
        var campaign = await _context.Campaigns.FindAsync(campaignId);
        if (campaign == null)
        {
            _logger.LogWarning("Campaign {CampaignId} not found when updating amount", campaignId);
            return;
        }

        campaign.CurrentAmount += amount;
        campaign.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Invalidate related caches
        await InvalidateActiveCampaignsCache();
        await InvalidateCampaignStatsCache(campaignId);

        _logger.LogInformation("Updated campaign {CampaignId} amount by {Amount}, new total: {Total}",
            campaignId, amount, campaign.CurrentAmount);
    }

    private static CampaignResponse MapToResponse(DonationCampaign campaign)
    {
        return new CampaignResponse
        {
            Id = campaign.Id,
            Title = campaign.Title,
            Description = campaign.Description,
            Goal = campaign.Goal,
            CurrentAmount = campaign.CurrentAmount,
            StartDate = campaign.StartDate,
            EndDate = campaign.EndDate,
            Status = campaign.Status,
            CreatedBy = campaign.CreatedBy,
            CreatedAt = campaign.CreatedAt,
            UpdatedAt = campaign.UpdatedAt,
            ProgressPercentage = campaign.ProgressPercentage,
            IsActive = campaign.IsActive,
            IsExpired = campaign.IsExpired,
            IsGoalReached = campaign.IsGoalReached,
            TimeRemaining = campaign.TimeRemaining,
            TotalDonations = 0 // We'll track this via events
        };
    }

    private async Task InvalidateActiveCampaignsCache()
    {
        if (_useRedis && _cache != null)
        {
            try
            {
                await _cache.RemoveAsync("active_campaigns");
                _logger.LogInformation("Invalidated active campaigns cache");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate active campaigns cache");
            }
        }
    }

    private async Task InvalidateCampaignStatsCache(int campaignId)
    {
        if (_useRedis && _cache != null)
        {
            try
            {
                await _cache.RemoveAsync($"campaign_stats_{campaignId}");
                _logger.LogInformation("Invalidated campaign {CampaignId} stats cache", campaignId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate campaign {CampaignId} stats cache", campaignId);
            }
        }
    }
}
