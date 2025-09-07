using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.OpenApi.Models;

namespace PaymentService.Configuration;

public class SwaggerConfiguration : DefaultOpenApiConfigurationOptions
{
    public override OpenApiInfo Info { get; set; } = new OpenApiInfo
    {
        Version = "v1.0.0",
        Title = "Payment Service API",
        Description = "A comprehensive payment processing service for the DonationBox system. " +
                     "This service handles donation payments using Saga orchestration patterns, " +
                     "implements distributed locking for scale-out scenarios, and maintains " +
                     "a complete payment ledger with outbox pattern for reliable event delivery.",
        Contact = new OpenApiContact
        {
            Name = "DonationBox Team",
            Email = "support@donationbox.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    };

    public override List<OpenApiServer> Servers { get; set; } = new List<OpenApiServer>
    {
        new OpenApiServer
        {
            Url = "http://localhost:7071",
            Description = "Local Development Server"
        },
        new OpenApiServer
        {
            Url = "https://paymentservice-dev.azurewebsites.net",
            Description = "Development Environment"
        },
        new OpenApiServer
        {
            Url = "https://paymentservice-prod.azurewebsites.net", 
            Description = "Production Environment"
        }
    };

    public override OpenApiVersionType OpenApiVersion { get; set; } = OpenApiVersionType.V3;

    public override bool IncludeRequestingHostName { get; set; } = false;
}
