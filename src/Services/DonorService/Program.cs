using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DonorService.Data;
using DonorService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Configure Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Server=(localdb)\\mssqllocaldb;Database=DonorServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<DonorDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Logging.AddConsole();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Register application services
builder.Services.AddScoped<IDonorService, DonorServiceImpl>();
builder.Services.AddScoped<IOrganizationService, OrganizationServiceImpl>();

// Register AuthService gRPC client
var authServiceUrl = builder.Configuration["AuthService:Url"] ?? "http://localhost:5001";
builder.Services.AddScoped(provider => new AuthGrpcClient(authServiceUrl, provider.GetRequiredService<ILogger<AuthGrpcClient>>()));

// Add gRPC services
builder.Services.AddGrpc();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Donor Service API",
        Version = "v1",
        Description = "A microservice for managing donors and their welfare organizations",
        Contact = new OpenApiContact
        {
            Name = "DonationBox Team",
            Email = "support@donationbox.com"
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
//builder.Services.AddHealthChecks()
//    .AddDbContextCheck<DonorDbContext>("database")
//    .AddSqlServer(connectionString, name: "sqlserver");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Donor Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthorization();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<GrpcDonorService>();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add a simple info endpoint
app.MapGet("/info", () => new
{
    Service = "DonorService",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow
})
.WithName("GetServiceInfo")
.WithOpenApi();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<DonorDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.Run();