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
            if (await context.Campaigns.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Seed sample data
            var sampleCampaigns = new List<DonationCampaign>
            {
                new DonationCampaign
                {
                    Title = "Help Build a Community Center",
                    Description = "We are raising funds to build a new community center that will serve local families with educational programs, recreational activities, and social services.",
                    Goal = 50000m,
                    CurrentAmount = 15000m,
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow.AddDays(60),
                    Status = CampaignStatus.Active,
                    CreatedBy = "community-admin",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                },
                new DonationCampaign
                {
                    Title = "Emergency Relief Fund",
                    Description = "Supporting families affected by recent natural disasters with emergency supplies, temporary housing, and essential services.",
                    Goal = 25000m,
                    CurrentAmount = 8500m,
                    StartDate = DateTime.UtcNow.AddDays(-15),
                    EndDate = DateTime.UtcNow.AddDays(45),
                    Status = CampaignStatus.Active,
                    CreatedBy = "relief-coordinator",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new DonationCampaign
                {
                    Title = "School Technology Upgrade",
                    Description = "Upgrading classroom technology and providing tablets for students to enhance digital learning opportunities.",
                    Goal = 75000m,
                    CurrentAmount = 75000m,
                    StartDate = DateTime.UtcNow.AddDays(-90),
                    EndDate = DateTime.UtcNow.AddDays(-10),
                    Status = CampaignStatus.Completed,
                    CreatedBy = "school-principal",
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                }
            };

            await context.Campaigns.AddRangeAsync(sampleCampaigns);
            await context.SaveChangesAsync();

            // Add sample donations
            var sampleDonations = new List<Donation>
            {
                new Donation
                {
                    CampaignId = 1,
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
                    CampaignId = 1,
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
                    CampaignId = 2,
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
