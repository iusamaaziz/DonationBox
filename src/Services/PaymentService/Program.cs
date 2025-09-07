using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentService.Configuration;
using PaymentService.Data;
using PaymentService.Services;
using StackExchange.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Configure Entity Framework
var connectionString = builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=PaymentServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Redis for distributed locking
var useRedis = builder.Configuration.GetValue<bool>("UseRedis", false);
if (useRedis)
{
    try
    {
        var redisConnectionString = builder.Configuration.GetValue<string>("ConnectionStrings:Redis") ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();
        
        Console.WriteLine($"Redis distributed locking enabled with connection: {redisConnectionString}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to configure Redis: {ex.Message}. Using in-memory distributed locking.");
        builder.Services.AddSingleton<IDistributedLockService, InMemoryDistributedLockService>();
    }
}
else
{
    builder.Services.AddSingleton<IDistributedLockService, InMemoryDistributedLockService>();
}

// Register application services
builder.Services.AddScoped<IPaymentGatewayService, SimulatedPaymentGatewayService>();
builder.Services.AddScoped<IOutboxService, OutboxService>();

// Configure Swagger/OpenAPI
builder.Services.AddSingleton<IOpenApiConfigurationOptions, SwaggerConfiguration>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services
    .AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Payment Service"
        });
    });

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.Run();
