using Microsoft.EntityFrameworkCore;
using DonorService.Models;

namespace DonorService.Data;

/// <summary>
/// Database initializer for the Donor Service
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initialize the database with schema and seed data
    /// </summary>
    /// <param name="context">The database context</param>
    public static async Task InitializeAsync(DonorDbContext context)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Run any pending migrations
            if ((await context.Database.GetPendingMigrationsAsync()).Any())
            {
                await context.Database.MigrateAsync();
            }

            // Check if data already exists
            if (await context.Donors.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Create sample donors
            var donors = new[]
            {
                new Donor
                {
                    UserId = "sample-user-1", // This should match AuthService user IDs
                    Bio = "Passionate about helping communities and supporting charitable causes.",
                    Interests = "[\"Education\",\"Healthcare\",\"Community Development\"]",
                    PhoneNumber = "+1-555-0101",
                    Address = "123 Charity Lane, Helping City, HC 12345",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Donor
                {
                    UserId = "sample-user-2",
                    Bio = "Dedicated to environmental conservation and wildlife protection.",
                    Interests = "[\"Environment\",\"Animal Welfare\",\"Conservation\"]",
                    PhoneNumber = "+1-555-0102",
                    Address = "456 Green Street, Eco Town, ET 67890",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Donors.AddRangeAsync(donors);
            await context.SaveChangesAsync();

            // Create sample organizations
            var organizations = new[]
            {
                new WelfareOrganization
                {
                    Name = "Community Helpers Foundation",
                    Description = "Dedicated to supporting local communities through various charitable programs.",
                    Type = "Foundation",
                    Mission = "To uplift communities and provide support where it's needed most.",
                    WebsiteUrl = "https://communityhelpers.org",
                    ContactEmail = "contact@communityhelpers.org",
                    ContactPhone = "+1-555-0200",
                    Address = "789 Foundation Blvd, Community City, CC 11111",
                    TaxId = "12-3456789",
                    CreatedByUserId = donors[0].UserId,
                    IsVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new WelfareOrganization
                {
                    Name = "Green Earth Alliance",
                    Description = "Environmental organization focused on conservation and sustainability.",
                    Type = "Environmental",
                    Mission = "Protecting our planet for future generations through conservation efforts.",
                    WebsiteUrl = "https://greenearthalliance.org",
                    ContactEmail = "info@greenearthalliance.org",
                    ContactPhone = "+1-555-0300",
                    Address = "321 Earth Way, Green Valley, GV 22222",
                    TaxId = "98-7654321",
                    CreatedByUserId = donors[1].UserId,
                    IsVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new WelfareOrganization
                {
                    Name = "Education First Initiative",
                    Description = "Promoting education and learning opportunities for underserved communities.",
                    Type = "Educational",
                    Mission = "Ensuring every child has access to quality education.",
                    WebsiteUrl = "https://educationfirst.org",
                    ContactEmail = "support@educationfirst.org",
                    ContactPhone = "+1-555-0400",
                    Address = "654 Learning Lane, Knowledge City, KC 33333",
                    TaxId = "55-1122334",
                    CreatedByUserId = donors[0].UserId,
                    IsVerified = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.WelfareOrganizations.AddRangeAsync(organizations);
            await context.SaveChangesAsync();

            Console.WriteLine("DonorService database initialized with sample data:");
            Console.WriteLine($"{donors.Length} donors created");
            Console.WriteLine($"{organizations.Length} organizations created");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while initializing the DonorService database: {ex.Message}");
            throw;
        }
    }
}
