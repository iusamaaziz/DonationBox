using Microsoft.EntityFrameworkCore;
using OrganizationService.Models;

namespace OrganizationService.Data;

/// <summary>
/// Entity Framework Core database context for Organization Service
/// </summary>
public class OrganizationDbContext : DbContext
{
    public OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Organizations table
    /// </summary>
    public DbSet<Organization> Organizations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Organization entity
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(o => o.Id);

            entity.Property(o => o.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(o => o.Description)
                .HasMaxLength(1000);

            entity.Property(o => o.Address)
                .HasMaxLength(500);

            entity.Property(o => o.Phone)
                .HasMaxLength(20);

            entity.Property(o => o.Email)
                .HasMaxLength(255);

            entity.Property(o => o.Website)
                .HasMaxLength(500);

            entity.Property(o => o.CreatedBy)
                .IsRequired();

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.Property(o => o.UpdatedAt)
                .IsRequired();

            entity.Property(o => o.IsActive)
                .IsRequired();

            // Index on CreatedBy for faster queries when filtering by user
            entity.HasIndex(o => o.CreatedBy);

            // Index on IsActive for faster queries when filtering active organizations
            entity.HasIndex(o => o.IsActive);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update the UpdatedAt property for all modified entities
        foreach (var entry in ChangeTracker.Entries<Organization>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

