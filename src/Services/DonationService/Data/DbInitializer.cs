using DonationService.Models;
using Microsoft.EntityFrameworkCore;

namespace DonationService.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(DonationDbContext context)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if database has been seeded
            if (await context.Donations.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Add sample donations (campaign validation should be done by CampaignService)
            var sampleDonations = new List<Donation>
            {
                new Donation
                {
                    CampaignId = 1, // Assuming campaign with ID 1 exists in CampaignService
                    Amount = 500m,
                    DonorName = "John Smith",
                    DonorEmail = "john.smith@email.com",
                    Message = "Great cause! Happy to contribute.",
                    IsAnonymous = false,
                    TransactionId = "TXN-001-" + Guid.NewGuid().ToString("N")[..8],
                    PaymentStatus = PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-25),
                    ProcessedAt = DateTime.UtcNow.AddDays(-25)
                },
                new Donation
                {
                    CampaignId = 1, // Assuming campaign with ID 1 exists in CampaignService
                    Amount = 1000m,
                    DonorName = "Anonymous Donor",
                    DonorEmail = "donor@email.com",
                    Message = "",
                    IsAnonymous = true,
                    TransactionId = "TXN-002-" + Guid.NewGuid().ToString("N")[..8],
                    PaymentStatus = PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    ProcessedAt = DateTime.UtcNow.AddDays(-20)
                },
                new Donation
                {
                    CampaignId = 2, // Assuming campaign with ID 2 exists in CampaignService
                    Amount = 250m,
                    DonorName = "Sarah Johnson",
                    DonorEmail = "sarah.j@email.com",
                    Message = "Thoughts and prayers with the affected families.",
                    IsAnonymous = false,
                    TransactionId = "TXN-003-" + Guid.NewGuid().ToString("N")[..8],
                    PaymentStatus = PaymentStatus.Completed,
                    CreatedAt = DateTime.UtcNow.AddDays(-12),
                    ProcessedAt = DateTime.UtcNow.AddDays(-12)
                }
            };

            await context.Donations.AddRangeAsync(sampleDonations);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Log the exception in a real application
            Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
            throw;
        }
    }
}
