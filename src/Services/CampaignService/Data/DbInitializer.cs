using CampaignService.Models;

namespace CampaignService.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(CampaignDbContext context)
    {
        try
        {
            // Create the database if it doesn't exist
            await context.Database.EnsureCreatedAsync();

            // Check if we already have data
            if (context.Campaigns.Any())
            {
                return; // Database has been seeded
            }

            // Seed with sample data
            var campaigns = new List<DonationCampaign>
            {
                new DonationCampaign
                {
                    Title = "Community Garden Project",
                    Description = "Help us build a community garden to provide fresh vegetables for local families in need.",
                    Goal = 5000.00m,
                    CurrentAmount = 1250.00m,
                    StartDate = DateTime.UtcNow.AddDays(-7),
                    EndDate = DateTime.UtcNow.AddDays(30),
                    Status = CampaignStatus.Active,
                    CreatedBy = "admin@donationbox.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow
                },
                new DonationCampaign
                {
                    Title = "School Library Renovation",
                    Description = "Support the renovation of our local elementary school's library to create a better learning environment for our children.",
                    Goal = 10000.00m,
                    CurrentAmount = 3200.00m,
                    StartDate = DateTime.UtcNow.AddDays(-14),
                    EndDate = DateTime.UtcNow.AddDays(60),
                    Status = CampaignStatus.Active,
                    CreatedBy = "school@donationbox.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-14),
                    UpdatedAt = DateTime.UtcNow
                },
                new DonationCampaign
                {
                    Title = "Animal Shelter Expansion",
                    Description = "Help us expand our animal shelter to accommodate more rescued animals and provide better care facilities.",
                    Goal = 15000.00m,
                    CurrentAmount = 8500.00m,
                    StartDate = DateTime.UtcNow.AddDays(-21),
                    EndDate = DateTime.UtcNow.AddDays(45),
                    Status = CampaignStatus.Active,
                    CreatedBy = "shelter@donationbox.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-21),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            context.Campaigns.AddRange(campaigns);
            await context.SaveChangesAsync();

            Console.WriteLine("Database seeded with sample campaign data.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
        }
    }
}
