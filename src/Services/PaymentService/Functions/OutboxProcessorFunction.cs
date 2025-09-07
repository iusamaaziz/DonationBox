using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using PaymentService.Services;

namespace PaymentService.Functions;

public class OutboxProcessorFunction
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<OutboxProcessorFunction> _logger;

    public OutboxProcessorFunction(IOutboxService outboxService, ILogger<OutboxProcessorFunction> logger)
    {
        _outboxService = outboxService;
        _logger = logger;
    }

    [Function("ProcessOutboxEvents")]
    public async Task ProcessOutboxEvents([TimerTrigger("0 */1 * * * *")] object myTimer)
    {
        _logger.LogInformation("Starting outbox event processing at {Time}", DateTime.UtcNow);

        try
        {
            await _outboxService.RetryFailedEventsAsync();
            _logger.LogInformation("Completed outbox event processing at {Time}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during outbox event processing");
        }
    }

    [Function("ProcessOutboxEventsManual")]
    public async Task<string> ProcessOutboxEventsManual(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "admin/process-outbox")] HttpRequestData req)
    {
        _logger.LogInformation("Manual outbox event processing triggered");

        try
        {
            var pendingEvents = await _outboxService.GetPendingEventsAsync();
            var eventCount = pendingEvents.Count();

            if (eventCount > 0)
            {
                await _outboxService.RetryFailedEventsAsync();
                _logger.LogInformation("Processed {EventCount} pending outbox events", eventCount);
                return $"Processed {eventCount} pending events";
            }
            else
            {
                _logger.LogInformation("No pending outbox events to process");
                return "No pending events to process";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual outbox event processing");
            return $"Error processing events: {ex.Message}";
        }
    }
}
