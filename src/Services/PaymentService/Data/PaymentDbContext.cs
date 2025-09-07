using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
    public DbSet<PaymentLedgerEntry> PaymentLedgerEntries { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PaymentTransaction entity
        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.DonorName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.DonorEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PaymentGateway).HasMaxLength(100);
            entity.Property(e => e.GatewayTransactionId).HasMaxLength(100);
            entity.Property(e => e.FailureReason).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Configure enums
            entity.Property(e => e.Status)
                .HasConversion<int>()
                .HasDefaultValue(PaymentStatus.Pending);
                
            entity.Property(e => e.PaymentMethod)
                .HasConversion<int>();

            // Indexes for performance
            entity.HasIndex(e => e.TransactionId).IsUnique();
            entity.HasIndex(e => e.DonationId);
            entity.HasIndex(e => e.CampaignId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.DonorEmail, e.CreatedAt });
        });

        // Configure PaymentLedgerEntry entity
        modelBuilder.Entity<PaymentLedgerEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
            entity.Property(e => e.Operation).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Metadata).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Configure enum
            entity.Property(e => e.EntryType)
                .HasConversion<int>();

            // Configure relationship
            entity.HasOne(e => e.PaymentTransaction)
                .WithMany(t => t.LedgerEntries)
                .HasForeignKey(e => e.PaymentTransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => e.PaymentTransactionId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntryType, e.CreatedAt });
        });

        // Configure OutboxEvent entity
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventData).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            
            // Configure enum
            entity.Property(e => e.Status)
                .HasConversion<int>()
                .HasDefaultValue(OutboxEventStatus.Pending);

            // Configure relationship
            entity.HasOne(e => e.PaymentTransaction)
                .WithMany(t => t.OutboxEvents)
                .HasForeignKey(e => e.PaymentTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes for performance
            entity.HasIndex(e => e.EventId).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.NextRetryAt);
            entity.HasIndex(e => new { e.Status, e.NextRetryAt });
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
        var paymentEntries = ChangeTracker.Entries<PaymentTransaction>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in paymentEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }

        var outboxEntries = ChangeTracker.Entries<OutboxEvent>()
            .Where(e => e.State == EntityState.Modified);

        foreach (var entry in outboxEntries)
        {
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
