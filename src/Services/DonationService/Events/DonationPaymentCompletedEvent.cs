namespace DonationService.Events;

public class DonationPaymentCompletedEvent
{
    public int CampaignId { get; set; }
    public int DonationId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public DateTime PaymentCompletedAt { get; set; }
    public string DonorEmail { get; set; } = string.Empty;
}
