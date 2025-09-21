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
    private readonly ILogger<DonationServiceImpl> _logger;

    public DonationServiceImpl(
        DonationDbContext context,
        IEventPublisher eventPublisher,
        ILogger<DonationServiceImpl> logger)
    {
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<Donation> CreateDonationAsync(CreateDonationRequest request)
    {
        // Note: Campaign validation should be done by CampaignService
        // For now, we'll assume the campaign ID is valid

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
        await PublishDonationPledgedEventAsync(donation);

        _logger.LogInformation("Created donation {DonationId} for campaign {CampaignId} with amount {Amount}",
            donation.Id, request.CampaignId, request.Amount);

        return donation;
    }

    public async Task<bool> ProcessDonationAsync(int donationId, string transactionId, PaymentStatus status)
    {
        var donation = await _context.Donations.FindAsync(donationId);

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

            // Publish donation payment completed event for eventual consistency
            await PublishDonationPaymentCompletedEventAsync(donation);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Processed donation {DonationId} with status {Status}", donationId, status);
        return true;
    }

    public async Task<IEnumerable<Donation>> GetDonationsByCampaignAsync(int campaignId)
    {
        return await _context.Donations
            .Where(d => d.CampaignId == campaignId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Donation?> GetDonationByIdAsync(int id)
    {
        return await _context.Donations.FindAsync(id);
    }

    public async Task<Donation?> GetDonationByTransactionIdAsync(string transactionId)
    {
        return await _context.Donations
            .FirstOrDefaultAsync(d => d.TransactionId == transactionId);
    }

    private async Task PublishDonationPledgedEventAsync(Donation donation)
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
            // Campaign details would need to be fetched from CampaignService if needed
            CampaignTitle = "Campaign details available via CampaignService",
            CampaignGoal = 0,
            CampaignCurrentAmount = 0,
            CampaignProgressPercentage = 0
        };

        await _eventPublisher.PublishAsync(donationEvent);
        _logger.LogInformation("Published DonationPledgedEvent for donation {DonationId}", donation.Id);
    }

    private async Task PublishDonationPaymentCompletedEventAsync(Donation donation)
    {
        var paymentEvent = new DonationPaymentCompletedEvent
        {
            DonationId = donation.Id,
            CampaignId = donation.CampaignId,
            Amount = donation.Amount,
            TransactionId = donation.TransactionId,
            PaymentCompletedAt = donation.ProcessedAt ?? DateTime.UtcNow,
            DonorEmail = donation.DonorEmail
        };

        await _eventPublisher.PublishAsync(paymentEvent);
        _logger.LogInformation("Published DonationPaymentCompletedEvent for donation {DonationId}", donation.Id);
    }

    private static string GenerateTransactionId()
    {
        return $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}
