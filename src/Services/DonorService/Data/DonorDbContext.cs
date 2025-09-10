using Microsoft.EntityFrameworkCore;
using DonorService.Models;

namespace DonorService.Data;

/// <summary>
/// Database context for the Donor Service
/// </summary>
public class DonorDbContext : DbContext
{
    public DonorDbContext(DbContextOptions<DonorDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Donors table
    /// </summary>
    public DbSet<Donor> Donors { get; set; } = null!;

    /// <summary>
    /// Welfare organizations table
    /// </summary>
    public DbSet<WelfareOrganization> WelfareOrganizations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Donor entity
        modelBuilder.Entity<Donor>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasMaxLength(36);
            entity.Property(e => e.Bio).HasMaxLength(1000);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // Configure WelfareOrganization entity
        modelBuilder.Entity<WelfareOrganization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(50).HasDefaultValue("Charity");
            entity.Property(e => e.Mission).HasMaxLength(500);
            entity.Property(e => e.WebsiteUrl).HasMaxLength(500);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);
            entity.Property(e => e.ContactPhone).HasMaxLength(20);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.TaxId).HasMaxLength(50);
            entity.Property(e => e.CreatedByUserId).IsRequired().HasMaxLength(36);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Configure relationship with Donor
            entity.HasOne(e => e.Creator)
                  .WithMany(d => d.Organizations)
                  .HasForeignKey(e => e.CreatedByUserId)
                  .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var donorEntries = ChangeTracker.Entries<Donor>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in donorEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        var orgEntries = ChangeTracker.Entries<WelfareOrganization>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in orgEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
