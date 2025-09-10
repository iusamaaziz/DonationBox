using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DonationService.Data;
using DonationService.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.Redis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Server=(localdb)\\mssqllocaldb;Database=DonationServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<DonationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Redis Cache (conditional based on UseRedis environment variable)
var useRedis = builder.Configuration.GetValue<bool>("UseRedis");
if (useRedis)
{
    try
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis") 
            ?? "localhost:6379";
        
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "DonationService";
        });
        
        Console.WriteLine($"Redis caching enabled with connection: {redisConnectionString}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Failed to configure Redis: {ex.Message}. Continuing without Redis caching.");
        useRedis = false;
    }
}

builder.Logging.AddConsole();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Register application services
builder.Services.AddScoped<ICampaignService, CampaignService>();
builder.Services.AddScoped<IDonationService, DonationServiceImpl>();
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// Register authentication services (gRPC)
builder.Services.AddScoped<IAuthValidationService, GrpcAuthValidationService>();

// Register DonorService gRPC client
var donorServiceUrl = builder.Configuration["DonorService:Url"] ?? "http://localhost:5004";
builder.Services.AddScoped(provider => new DonationService.Services.DonorClient(donorServiceUrl, provider.GetRequiredService<ILogger<DonationService.Services.DonorClient>>()));

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Donation Service API",
        Version = "v1",
        Description = "A microservice for managing donation campaigns and donations with JWT authentication",
        Contact = new OpenApiContact
        {
            Name = "DonationBox Team",
            Email = "support@donationbox.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header,
            },
            new List<string>()
        }
    });

    // Include XML comments for better API documentation
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// Configure CORS for microservice communication
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Donation Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add a simple info endpoint
app.MapGet("/info", () => new
{
    Service = "DonationService",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    UseRedis = useRedis,
    Timestamp = DateTime.UtcNow
})
.WithName("GetServiceInfo")
.WithOpenApi();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DonationDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.Run();
