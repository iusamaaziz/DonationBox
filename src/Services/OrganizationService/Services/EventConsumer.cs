using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace OrganizationService.Services;

/// <summary>
/// Background service for consuming events
/// </summary>
public class EventConsumer : BackgroundService, IEventConsumer
{
    private readonly ILogger<EventConsumer> _logger;

    public EventConsumer(ILogger<EventConsumer> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventConsumer started");

        // TODO: Implement event consumption logic
        // This could be for handling events from message queues, etc.

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }

        _logger.LogInformation("EventConsumer stopped");
    }
}
