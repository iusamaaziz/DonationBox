using System.Text.Json;

namespace DonationService.Services;

public class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly IConfiguration _configuration;

    public EventPublisher(ILogger<EventPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task PublishAsync<T>(T eventData) where T : class
    {
        try
        {
            // In a real implementation, this would publish to a message broker like:
            // - Azure Service Bus
            // - RabbitMQ
            // - Apache Kafka
            // - Redis Pub/Sub
            // 
            // For now, we'll log the event and simulate async publishing
            
            var eventName = typeof(T).Name;
            var eventJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            _logger.LogInformation("Publishing event {EventName}: {EventData}", eventName, eventJson);

            // Simulate async operation
            await Task.Delay(10);

            // In production, you would:
            // 1. Serialize the event
            // 2. Send to message broker
            // 3. Handle failures with retry logic
            // 4. Implement dead letter queues for failed messages
            
            _logger.LogInformation("Successfully published event {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType}", typeof(T).Name);
            
            // In production, you might want to:
            // 1. Store failed events in a database for retry
            // 2. Implement circuit breaker pattern
            // 3. Send to dead letter queue
            throw;
        }
    }
}
