using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

using PaymentService.Data;
using PaymentService.Services;

using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddNewtonsoftJson();

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=PaymentServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Redis for distributed locking
var useRedis = builder.Configuration.GetValue<bool>("UseRedis", false);
if (useRedis)
{
    try
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        var multiplexer = ConnectionMultiplexer.Connect(redisConnectionString);
        builder.Services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        builder.Services.AddSingleton<IDistributedLockService, RedisDistributedLockService>();

        builder.Logging.AddConsole();
        builder.Services.AddLogging();
    }
    catch (Exception ex)
    {
        // Redis configuration failed, log will be handled by the logging framework when service starts
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

// Register PaymentSagaService as singleton and hosted service
builder.Services.AddSingleton<PaymentSagaService>();
builder.Services.AddSingleton<IPaymentSagaService>(sp => sp.GetRequiredService<PaymentSagaService>());
builder.Services.AddHostedService(sp => sp.GetRequiredService<PaymentSagaService>());

// Register OutboxProcessorBackgroundService
builder.Services.AddHostedService<OutboxProcessorBackgroundService>();

// Configure logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Payment Service API",
        Version = "v1",
        Description = "Payment processing service with saga orchestration"
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service API v1");
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.Run();
