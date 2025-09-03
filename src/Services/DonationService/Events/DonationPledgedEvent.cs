namespace DonationService.Events;

public class DonationPledgedEvent
{
    public int DonationId { get; set; }
    public int CampaignId { get; set; }
    public decimal Amount { get; set; }
    public string DonorName { get; set; } = string.Empty;
    public string DonorEmail { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsAnonymous { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CampaignTitle { get; set; } = string.Empty;
    public decimal CampaignGoal { get; set; }
    public decimal CampaignCurrentAmount { get; set; }
    public decimal CampaignProgressPercentage { get; set; }
}
