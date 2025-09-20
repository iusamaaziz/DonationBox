using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrganizationService.Models;

namespace OrganizationService.Data;

/// <summary>
/// Database initializer for seeding initial data
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initializes the database with sample data
    /// </summary>
    public static async Task InitializeAsync(OrganizationDbContext _context)
    {
        try
        {
            // Ensure database is created
            await _context.Database.EnsureCreatedAsync();

            // Run any pending migrations
            if ((await _context.Database.GetPendingMigrationsAsync()).Any())
            {
                await _context.Database.MigrateAsync();
            }

            // Check if we need to seed data
            if (!await _context.Organizations.AnyAsync())
            {
                // Sample organizations - in a real app, these would come from configuration or be optional
                var sampleOrganizations = new List<Organization>
        {
            new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Hope for Tomorrow Foundation",
                Description = "A nonprofit organization dedicated to providing food and shelter to families in need.",
                Address = "123 Charity Street, Hope City, HC 12345",
                Phone = "(555) 123-4567",
                Email = "info@hopefortomorrow.org",
                Website = "https://www.hopefortomorrow.org",
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000001"), // Sample user ID
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-30),
                IsActive = true
            },
            new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Green Earth Initiative",
                Description = "Environmental conservation organization focused on protecting wildlife and natural habitats.",
                Address = "456 Eco Lane, Green Valley, GV 67890",
                Phone = "(555) 987-6543",
                Email = "contact@greenearth.org",
                Website = "https://www.greenearth.org",
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000002"), // Sample user ID
                CreatedAt = DateTime.UtcNow.AddDays(-15),
                UpdatedAt = DateTime.UtcNow.AddDays(-15),
                IsActive = true
            },
            new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Youth Education Alliance",
                Description = "Supporting educational opportunities for underprivileged youth through scholarships and tutoring programs.",
                Address = "789 Learning Blvd, Education City, EC 54321",
                Phone = "(555) 456-7890",
                Email = "info@youtheducation.org",
                Website = "https://www.youtheducation.org",
                CreatedBy = Guid.Parse("00000000-0000-0000-0000-000000000003"), // Sample user ID
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow.AddDays(-7),
                IsActive = true
            }
        };

                await _context.Organizations.AddRangeAsync(sampleOrganizations);
                await _context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding the database: {ex.Message}");
        }
    }
}

