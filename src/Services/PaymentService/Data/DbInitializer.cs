using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(PaymentDbContext context)
    {
        try
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Check if database has been seeded
            if (await context.PaymentTransactions.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Seed sample data for testing
            var sampleTransactions = new List<PaymentTransaction>
            {
                new PaymentTransaction
                {
                    TransactionId = "TXN-PAY-001-" + Guid.NewGuid().ToString("N")[..8],
                    DonationId = 1,
                    CampaignId = 1,
                    Amount = 500.00m,
                    Currency = "USD",
                    DonorName = "John Smith",
                    DonorEmail = "john.smith@email.com",
                    Status = PaymentStatus.Completed,
                    PaymentMethod = PaymentMethod.CreditCard,
                    PaymentGateway = "Stripe",
                    GatewayTransactionId = "pi_" + Guid.NewGuid().ToString("N")[..24],
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    ProcessedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(2),
                    CompletedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(5)
                },
                new PaymentTransaction
                {
                    TransactionId = "TXN-PAY-002-" + Guid.NewGuid().ToString("N")[..8],
                    DonationId = 2,
                    CampaignId = 1,
                    Amount = 1000.00m,
                    Currency = "USD",
                    DonorName = "Sarah Johnson",
                    DonorEmail = "sarah.j@email.com",
                    Status = PaymentStatus.Completed,
                    PaymentMethod = PaymentMethod.PayPal,
                    PaymentGateway = "PayPal",
                    GatewayTransactionId = "PAYID-" + Guid.NewGuid().ToString("N")[..20],
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    ProcessedAt = DateTime.UtcNow.AddDays(-3).AddMinutes(1),
                    CompletedAt = DateTime.UtcNow.AddDays(-3).AddMinutes(3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3).AddMinutes(3)
                },
                new PaymentTransaction
                {
                    TransactionId = "TXN-PAY-003-" + Guid.NewGuid().ToString("N")[..8],
                    DonationId = 3,
                    CampaignId = 2,
                    Amount = 250.00m,
                    Currency = "USD",
                    DonorName = "Mike Wilson",
                    DonorEmail = "mike.w@email.com",
                    Status = PaymentStatus.Failed,
                    PaymentMethod = PaymentMethod.CreditCard,
                    PaymentGateway = "Stripe",
                    FailureReason = "Insufficient funds",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    ProcessedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1).AddMinutes(1)
                }
            };

            await context.PaymentTransactions.AddRangeAsync(sampleTransactions);
            await context.SaveChangesAsync();

            // Add ledger entries for completed transactions
            var ledgerEntries = new List<PaymentLedgerEntry>();

            foreach (var transaction in sampleTransactions.Where(t => t.Status == PaymentStatus.Completed))
            {
                // Payment entry
                ledgerEntries.Add(new PaymentLedgerEntry
                {
                    PaymentTransactionId = transaction.Id,
                    TransactionId = transaction.TransactionId,
                    Amount = transaction.Amount,
                    EntryType = LedgerEntryType.Payment,
                    Operation = "DEBIT",
                    Description = $"Payment for donation {transaction.DonationId}",
                    CreatedAt = transaction.CompletedAt ?? transaction.CreatedAt
                });

                // Processing fee entry (simulate 2.9% + $0.30 fee)
                var fee = Math.Round(transaction.Amount * 0.029m + 0.30m, 2);
                ledgerEntries.Add(new PaymentLedgerEntry
                {
                    PaymentTransactionId = transaction.Id,
                    TransactionId = transaction.TransactionId + "-FEE",
                    Amount = fee,
                    EntryType = LedgerEntryType.Fee,
                    Operation = "DEBIT",
                    Description = "Payment processing fee",
                    CreatedAt = transaction.CompletedAt ?? transaction.CreatedAt
                });
            }

            if (ledgerEntries.Any())
            {
                await context.PaymentLedgerEntries.AddRangeAsync(ledgerEntries);
                await context.SaveChangesAsync();
            }

            Console.WriteLine("Payment database initialized with sample data");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while seeding the payment database: {ex.Message}");
            throw;
        }
    }
}
