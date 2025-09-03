using Microsoft.EntityFrameworkCore;
using DonationService.Data;
using DonationService.DTOs;
using DonationService.Events;
using DonationService.Models;

namespace DonationService.Services;

public class DonationServiceImpl : IDonationService
{
    private readonly DonationDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICampaignService _campaignService;
    private readonly ILogger<DonationServiceImpl> _logger;

    public DonationServiceImpl(
        DonationDbContext context,
        IEventPublisher eventPublisher,
        ICampaignService campaignService,
        ILogger<DonationServiceImpl> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _campaignService = campaignService;
        _logger = logger;
    }

    public async Task<Donation> CreateDonationAsync(CreateDonationRequest request)
    {
        // Validate campaign exists and is active
        var campaign = await _context.Campaigns.FindAsync(request.CampaignId);
        if (campaign == null)
        {
            throw new ArgumentException($"Campaign with ID {request.CampaignId} not found");
        }

        if (!campaign.IsActive)
        {
            throw new InvalidOperationException($"Campaign '{campaign.Title}' is not currently accepting donations");
        }

        // Create donation
        var donation = new Donation
        {
            CampaignId = request.CampaignId,
            Amount = request.Amount,
            DonorName = request.DonorName,
            DonorEmail = request.DonorEmail,
            Message = request.Message,
            IsAnonymous = request.IsAnonymous,
            TransactionId = GenerateTransactionId(),
            PaymentStatus = PaymentStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _context.Donations.Add(donation);
        await _context.SaveChangesAsync();

        // Publish donation pledged event
        await PublishDonationPledgedEventAsync(donation, campaign);

        _logger.LogInformation("Created donation {DonationId} for campaign {CampaignId} with amount {Amount}",
            donation.Id, request.CampaignId, request.Amount);

        return donation;
    }

    public async Task<bool> ProcessDonationAsync(int donationId, string transactionId, PaymentStatus status)
    {
        var donation = await _context.Donations
            .Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.Id == donationId);

        if (donation == null)
        {
            _logger.LogWarning("Donation {DonationId} not found for processing", donationId);
            return false;
        }

        // Update donation status
        donation.PaymentStatus = status;
        donation.TransactionId = transactionId;
        
        if (status == PaymentStatus.Completed)
        {
            donation.ProcessedAt = DateTime.UtcNow;
            
            // Update campaign current amount
            donation.Campaign.CurrentAmount += donation.Amount;
            donation.Campaign.UpdatedAt = DateTime.UtcNow;

            // Refresh campaign stats cache
            await _campaignService.RefreshCampaignStatsAsync(donation.CampaignId);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed donation {DonationId} with status {Status}", donationId, status);
        return true;
    }

    public async Task<IEnumerable<Donation>> GetDonationsByCampaignAsync(int campaignId)
    {
        return await _context.Donations
            .Include(d => d.Campaign)
            .Where(d => d.CampaignId == campaignId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Donation?> GetDonationByIdAsync(int id)
    {
        return await _context.Donations
            .Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Donation?> GetDonationByTransactionIdAsync(string transactionId)
    {
        return await _context.Donations
            .Include(d => d.Campaign)
            .FirstOrDefaultAsync(d => d.TransactionId == transactionId);
    }

    private async Task PublishDonationPledgedEventAsync(Donation donation, DonationCampaign campaign)
    {
        var donationEvent = new DonationPledgedEvent
        {
            DonationId = donation.Id,
            CampaignId = donation.CampaignId,
            Amount = donation.Amount,
            DonorName = donation.DonorName,
            DonorEmail = donation.DonorEmail,
            Message = donation.Message,
            IsAnonymous = donation.IsAnonymous,
            TransactionId = donation.TransactionId,
            CreatedAt = donation.CreatedAt,
            CampaignTitle = campaign.Title,
            CampaignGoal = campaign.Goal,
            CampaignCurrentAmount = campaign.CurrentAmount,
            CampaignProgressPercentage = campaign.ProgressPercentage
        };

        await _eventPublisher.PublishAsync(donationEvent);
        _logger.LogInformation("Published DonationPledgedEvent for donation {DonationId}", donation.Id);
    }

    private static string GenerateTransactionId()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
