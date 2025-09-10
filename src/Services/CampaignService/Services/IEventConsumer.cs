using CampaignService.Events;

namespace CampaignService.Services;

public interface IEventConsumer
{
    Task StartConsumingAsync();
    Task StopConsumingAsync();
    Task ProcessDonationPaymentCompletedEventAsync(DonationPaymentCompletedEvent eventData);
}
