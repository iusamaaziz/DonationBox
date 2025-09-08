using Microsoft.EntityFrameworkCore;
using AuthService.Models;
using BCrypt.Net;

namespace AuthService.Data;

/// <summary>
/// Database initializer for the Authentication Service
/// </summary>
public static class DbInitializer
{
    /// <summary>
    /// Initialize the database with schema and seed data
    /// </summary>
    /// <param name="context">The database context</param>
    public static async Task InitializeAsync(AuthDbContext context)
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
            if (await context.Users.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Create sample users
            var users = new[]
            {
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin@donationbox.com",
                    FirstName = "Admin",
                    LastName = "User",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    Provider = "Local",
                    EmailVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "john.doe@example.com",
                    FirstName = "John",
                    LastName = "Doe",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    Provider = "Local",
                    EmailVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "jane.smith@example.com",
                    FirstName = "Jane",
                    LastName = "Smith",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
                    Provider = "Local",
                    EmailVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Email = "google.user@gmail.com",
                    FirstName = "Google",
                    LastName = "User",
                    Provider = "Google",
                    ProviderId = "google_123456789",
                    EmailVerified = true,
                    IsActive = true,
                    ProfilePictureUrl = "https://lh3.googleusercontent.com/a/default-user",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();

            Console.WriteLine("Database initialized with sample users:");
            foreach (var user in users)
            {
                Console.WriteLine($"- {user.FullName} ({user.Email}) - Provider: {user.Provider}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while initializing the database: {ex.Message}");
            throw;
        }
    }
}
