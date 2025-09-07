using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PaymentService.Services;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IOutboxService _outboxService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IOutboxService outboxService, ILogger<AdminController> logger)
    {
        _outboxService = outboxService;
        _logger = logger;
    }

    [HttpPost("process-outbox")]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<IActionResult> ProcessOutboxEvents()
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
                return Ok($"Processed {eventCount} pending events");
            }
            else
            {
                _logger.LogInformation("No pending outbox events to process");
                return Ok("No pending events to process");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during manual outbox event processing");
            return StatusCode(500, $"Error processing events: {ex.Message}");
        }
    }
}
