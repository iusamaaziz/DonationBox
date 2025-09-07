using PaymentService.Models;

namespace PaymentService.Services;

public interface IOutboxService
{
    Task PublishEventAsync<T>(T eventData) where T : class;
    Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 50);
    Task MarkEventAsProcessedAsync(int eventId);
    Task MarkEventAsFailedAsync(int eventId, string errorMessage);
    Task RetryFailedEventsAsync();
}
