using DonationService.Models;

namespace DonationService.DTOs;

public class DonationResponse
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public decimal Amount { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public PaymentStatus PaymentStatus { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    
    // Campaign summary (fetched from CampaignService when needed)
    public CampaignSummary? Campaign { get; set; }
}

public class CampaignSummary
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Goal { get; set; }
    public decimal CurrentAmount { get; set; }
}
