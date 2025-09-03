using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.Services;

public interface ICampaignService
{
    Task<IEnumerable<CampaignResponse>> GetAllCampaignsAsync();
    Task<IEnumerable<CampaignResponse>> GetActiveCampaignsAsync();
    Task<CampaignResponse?> GetCampaignByIdAsync(int id);
    Task<CampaignStatsResponse?> GetCampaignStatsAsync(int id);
    Task<CampaignResponse> CreateCampaignAsync(CreateCampaignRequest request);
    Task<CampaignResponse?> UpdateCampaignAsync(int id, UpdateCampaignRequest request);
    Task<bool> DeleteCampaignAsync(int id);
    Task<bool> UpdateCampaignStatusAsync(int id, CampaignStatus status);
    Task RefreshCampaignStatsAsync(int campaignId);
    Task<IEnumerable<CampaignResponse>> GetCampaignsByCreatorAsync(string createdBy);
}
