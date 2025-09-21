using Microsoft.EntityFrameworkCore;
using DonationService.Models;

namespace DonationService.Data;

public class DonationDbContext : DbContext
{
    public DonationDbContext(DbContextOptions<DonationDbContext> options) : base(options)
    {
    }

    public DbSet<Donation> Donations { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Donation entity
        modelBuilder.Entity<Donation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.DonorName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DonorEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.TransactionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

            // Configure enum
            entity.Property(e => e.PaymentStatus)
                .HasConversion<int>()
                .HasDefaultValue(PaymentStatus.Pending);

            // Indexes for performance
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.PaymentStatus);
            entity.HasIndex(e => e.CreatedAt);
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
        // No timestamp updates needed for Donation entity
    }
}
