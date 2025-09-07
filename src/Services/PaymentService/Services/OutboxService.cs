using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using PaymentService.Data;
using PaymentService.Models;

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

        // Event will be processed by the background service (PaymentSagaService or separate outbox processor)
        // This avoids DbContext disposal issues with immediate processing
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
        // Events are now processed by the background service (OutboxProcessorBackgroundService)
        // This method is kept for backward compatibility but doesn't do immediate processing
        // to avoid DbContext disposal issues

        var pendingEvents = await GetPendingEventsAsync();
        _logger.LogInformation("Found {Count} pending outbox events for background processing", pendingEvents.Count());
    }


    private DateTime CalculateNextRetryTime(int retryCount)
    {
        // Exponential backoff: 2^retryCount minutes, max 60 minutes
        var delayMinutes = Math.Min(Math.Pow(2, retryCount), 60);
        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }
}
