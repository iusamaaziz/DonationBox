using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Services;

public class OutboxProcessorBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            // Wait before next processing cycle
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Background Service is stopping");
    }

    private async Task ProcessPendingEventsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxService = scope.ServiceProvider.GetRequiredService<IOutboxService>();

        var pendingEvents = await outboxService.GetPendingEventsAsync(10); // Process 10 events at a time

        if (!pendingEvents.Any())
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count());

        foreach (var eventItem in pendingEvents)
        {
            try
            {
                await ProcessEventAsync(scope.ServiceProvider, eventItem);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process outbox event {EventId}", eventItem.EventId);

                // Mark event as failed
                await outboxService.MarkEventAsFailedAsync(eventItem.Id, ex.Message);
            }
        }
    }

    private async Task ProcessEventAsync(IServiceProvider serviceProvider, OutboxEvent outboxEvent)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var logger = scope.ServiceProvider.GetService<ILogger<OutboxProcessorBackgroundService>>() ?? _logger;

        try
        {
            logger.LogInformation("Processing outbox event {EventId} of type {EventType}",
                outboxEvent.EventId, outboxEvent.EventType);

            // Update status to processing
            outboxEvent.Status = OutboxEventStatus.Processing;
            outboxEvent.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            // In a real implementation, this would:
            // 1. Send to message broker (Azure Service Bus, RabbitMQ, etc.)
            // 2. Call external APIs
            // 3. Trigger other services

            // For simulation, we'll just log and mark as completed
            await SimulateEventProcessingAsync(outboxEvent);

            // Mark as completed
            outboxEvent.Status = OutboxEventStatus.Completed;
            outboxEvent.ProcessedAt = DateTime.UtcNow;
            outboxEvent.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            logger.LogInformation("Successfully processed outbox event {EventId}", outboxEvent.EventId);
        }
        catch (Exception ex)
        {
            // Mark as failed
            outboxEvent.Status = OutboxEventStatus.Failed;
            outboxEvent.RetryCount++;
            outboxEvent.ErrorMessage = ex.Message;
            outboxEvent.NextRetryAt = CalculateNextRetryTime(outboxEvent.RetryCount);
            outboxEvent.UpdatedAt = DateTime.UtcNow;

            // Cancel event if max retries exceeded (hardcoded to 5 for now)
            if (outboxEvent.RetryCount >= 5)
            {
                outboxEvent.Status = OutboxEventStatus.Cancelled;
                outboxEvent.NextRetryAt = null;
                logger.LogWarning("Event {EventId} cancelled after {RetryCount} failed attempts",
                    outboxEvent.EventId, outboxEvent.RetryCount);
            }

            await context.SaveChangesAsync();

            logger.LogWarning("Marked event {EventId} as failed (attempt {RetryCount}): {Error}",
                outboxEvent.EventId, outboxEvent.RetryCount, ex.Message);

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

        _logger.LogInformation("Successfully processed event {EventId} of type {EventType}",
            outboxEvent.EventId, outboxEvent.EventType);
    }

    private DateTime CalculateNextRetryTime(int retryCount)
    {
        // Exponential backoff: 2^retryCount minutes, max 60 minutes
        var delayMinutes = Math.Min(Math.Pow(2, retryCount), 60);
        return DateTime.UtcNow.AddMinutes(delayMinutes);
    }
}
