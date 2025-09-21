using CampaignService.Events;

namespace CampaignService.Services;

public class EventConsumer : IEventConsumer, IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventConsumer> _logger;
    private readonly IConfiguration _configuration;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _consumerTask;

    public EventConsumer(
        IServiceProvider serviceProvider,
        ILogger<EventConsumer> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartConsumingAsync()
    {
        _logger.LogInformation("Starting event consumer...");

        // In a real implementation, this would connect to a message broker
        // For now, we'll simulate event consumption

        _cancellationTokenSource = new CancellationTokenSource();
        _consumerTask = Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // Simulate checking for new events
                    // In production, this would poll from a message queue
                    await Task.Delay(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token);

                    // For demonstration, we'll log that we're checking for events
                    _logger.LogDebug("Checking for new donation events...");
                }
                catch (TaskCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in event consumer loop");
                    await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
                }
            }
        }, _cancellationTokenSource.Token);

        _logger.LogInformation("Event consumer started successfully");
    }

    public async Task StopConsumingAsync()
    {
        _logger.LogInformation("Stopping event consumer...");

        if (_cancellationTokenSource != null)
        {
            _cancellationTokenSource.Cancel();
        }

        if (_consumerTask != null)
        {
            try
            {
                await _consumerTask.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Event consumer did not stop gracefully within timeout");
            }
        }

        _logger.LogInformation("Event consumer stopped");
    }

    public async Task ProcessDonationPaymentCompletedEventAsync(DonationPaymentCompletedEvent eventData)
    {
        try
        {
            _logger.LogInformation("Processing donation payment completed event for campaign {CampaignId}, donation {DonationId}, amount {Amount}",
                eventData.CampaignId, eventData.DonationId, eventData.Amount);

            // Create a scope to resolve the scoped ICampaignService
            using (var scope = _serviceProvider.CreateScope())
            {
                var campaignService = scope.ServiceProvider.GetRequiredService<ICampaignService>();

                // Update campaign amount
                await campaignService.UpdateCampaignAmountAsync(eventData.CampaignId, eventData.Amount);
            }

            _logger.LogInformation("Successfully processed donation payment event for campaign {CampaignId}",
                eventData.CampaignId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process donation payment completed event for campaign {CampaignId}",
                eventData.CampaignId);
            throw;
        }
    }

    // IHostedService implementation
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return StartConsumingAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return StopConsumingAsync();
    }
}
