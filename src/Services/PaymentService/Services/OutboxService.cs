using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PaymentService.Data;
using PaymentService.Models;
using System.Text.Json;

namespace PaymentService.Services;

public class OutboxService : IOutboxService
{
    private readonly PaymentDbContext _context;
    private readonly ILogger<OutboxService> _logger;
    private readonly IConfiguration _configuration;

    public OutboxService(
        PaymentDbContext context,
        ILogger<OutboxService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task PublishEventAsync<T>(T eventData) where T : class
    {
        var eventId = Guid.NewGuid().ToString();
        var eventType = typeof(T).Name;
        var eventDataJson = JsonSerializer.Serialize(eventData, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var outboxEvent = new OutboxEvent
        {
            EventId = eventId,
            EventType = eventType,
            EventData = eventDataJson,
            Status = OutboxEventStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Added event {EventId} of type {EventType} to outbox", eventId, eventType);

        // Immediately try to process the event
        _ = Task.Run(async () =>
        {
            try
            {
                await ProcessEventAsync(outboxEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to immediately process event {EventId}", eventId);
            }
        });
    }

    public async Task<IEnumerable<OutboxEvent>> GetPendingEventsAsync(int batchSize = 50)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == OutboxEventStatus.Pending || 
                       (e.Status == OutboxEventStatus.Failed && e.NextRetryAt <= DateTime.UtcNow))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task MarkEventAsProcessedAsync(int eventId)
    {
        var outboxEvent = await _context.OutboxEvents.FindAsync(eventId);
        if (outboxEvent != null)
        {
            outboxEvent.Status = OutboxEventStatus.Completed;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Marked event {EventId} as processed", outboxEvent.EventId);
        }
    }

    public async Task MarkEventAsFailedAsync(int eventId, string errorMessage)
    {
        var outboxEvent = await _context.OutboxEvents.FindAsync(eventId);
        if (outboxEvent != null)
        {
            outboxEvent.Status = OutboxEventStatus.Failed;
            outboxEvent.RetryCount++;
            outboxEvent.ErrorMessage = errorMessage;
            outboxEvent.NextRetryAt = CalculateNextRetryTime(outboxEvent.RetryCount);
            outboxEvent.UpdatedAt = DateTime.UtcNow;

            // Cancel event if max retries exceeded
            var maxRetries = _configuration.GetValue<int>("Outbox:MaxRetries", 5);
            if (outboxEvent.RetryCount >= maxRetries)
            {
                outboxEvent.Status = OutboxEventStatus.Cancelled;
                outboxEvent.NextRetryAt = null;
                _logger.LogWarning("Event {EventId} cancelled after {RetryCount} failed attempts", 
                    outboxEvent.EventId, outboxEvent.RetryCount);
            }

            await _context.SaveChangesAsync();

            _logger.LogWarning("Marked event {EventId} as failed (attempt {RetryCount}): {Error}", 
                outboxEvent.EventId, outboxEvent.RetryCount, errorMessage);
        }
    }

    public async Task RetryFailedEventsAsync()
    {
        var failedEvents = await GetPendingEventsAsync();
        
        foreach (var eventItem in failedEvents)
        {
            try
            {
                await ProcessEventAsync(eventItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry event {EventId}", eventItem.EventId);
            }
        }
    }

    private async Task ProcessEventAsync(OutboxEvent outboxEvent)
    {
        try
        {
            _logger.LogInformation("Processing outbox event {EventId} of type {EventType}", 
                outboxEvent.EventId, outboxEvent.EventType);

            // Update status to processing
            outboxEvent.Status = OutboxEventStatus.Processing;
            outboxEvent.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // In a real implementation, this would:
            // 1. Send to message broker (Azure Service Bus, RabbitMQ, etc.)
            // 2. Call external APIs
            // 3. Trigger other services
            
            // For simulation, we'll just log and mark as completed
            await SimulateEventProcessingAsync(outboxEvent);

            await MarkEventAsProcessedAsync(outboxEvent.Id);
        }
        catch (Exception ex)
        {
            await MarkEventAsFailedAsync(outboxEvent.Id, ex.Message);
            throw;
        }
    }

    private async Task SimulateEventProcessingAsync(OutboxEvent outboxEvent)
    {
        // Simulate processing time
        await Task.Delay(Random.Shared.Next(100, 500));

        // Simulate occasional failures (5% failure rate)
        if (Random.Shared.NextDouble() < 0.05)
        {
            throw new InvalidOperationException("Simulated event processing failure");
        }

        _logger.LogInformation("Successfully processed event {EventId} of type {EventType}: {EventData}", 
            outboxEvent.EventId, outboxEvent.EventType, outboxEvent.EventData);
    }

    private DateTime CalculateNextRetryTime(int retryCount)
    {
        // Exponential backoff: 2^retryCount minutes, max 60 minutes
        var delayMinutes = Math.Min(Math.Pow(2, retryCount), 60);
        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }
}
