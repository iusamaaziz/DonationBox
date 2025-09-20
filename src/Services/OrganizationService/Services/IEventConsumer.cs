using Microsoft.Extensions.Hosting;

namespace OrganizationService.Services;

/// <summary>
/// Interface for event consumers
/// </summary>
public interface IEventConsumer : IHostedService
{
}
