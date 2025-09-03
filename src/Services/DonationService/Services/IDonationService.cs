using DonationService.DTOs;
using DonationService.Models;

namespace DonationService.Services;

public interface IDonationService
{
    Task<Donation> CreateDonationAsync(CreateDonationRequest request);
    Task<bool> ProcessDonationAsync(int donationId, string transactionId, PaymentStatus status);
    Task<IEnumerable<Donation>> GetDonationsByCampaignAsync(int campaignId);
    Task<Donation?> GetDonationByIdAsync(int id);
    Task<Donation?> GetDonationByTransactionIdAsync(string transactionId);
}
