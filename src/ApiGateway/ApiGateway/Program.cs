using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire components
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "DonationBox API Gateway",
        Version = "v1",
        Description = "API Gateway for DonationBox microservices using YARP",
        Contact = new()
        {
            Name = "DonationBox Team",
            Email = "support@donationbox.com"
        }
    });
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var corsSettings = builder.Configuration.GetSection("CorsSettings");
        var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        var allowedMethods = corsSettings.GetSection("AllowedMethods").Get<string[]>() ?? Array.Empty<string>();
        var allowedHeaders = corsSettings.GetSection("AllowedHeaders").Get<string[]>() ?? Array.Empty<string>();
        var allowCredentials = corsSettings.GetValue<bool>("AllowCredentials");

        policy.WithOrigins(allowedOrigins)
              .WithMethods(allowedMethods)
              .WithHeaders(allowedHeaders);

        if (allowCredentials)
        {
            policy.AllowCredentials();
        }
    });
});

// Configure JWT Authentication for API Gateway
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-jwt-key-that-is-at-least-256-bits-long-for-gateway!";
var issuer = jwtSettings["Issuer"] ?? "ApiGateway";
var audience = jwtSettings["Audience"] ?? "DonationBoxServices";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // Forward the token to downstream services
    options.ForwardDefaultSelector = context =>
    {
        return context.Request.Headers.Authorization.FirstOrDefault();
    };
});

// Configure Health Checks (additional to service defaults)
builder.Services.AddHealthChecks()
    .AddCheck("gateway", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("API Gateway is running"));

// Configure Logging
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DonationBox API Gateway v1");
        c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

// Enable CORS
app.UseCors();

// Enable Authentication and Authorization
app.UseAuthentication();
app.UseAuthorization();

// Add custom middleware for request logging
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Incoming request: {Method} {Path}", context.Request.Method, context.Request.Path);

    await next();

    logger.LogInformation("Outgoing response: {StatusCode}", context.Response.StatusCode);
});

// Map YARP Reverse Proxy
app.MapReverseProxy();

// Map default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

// Add custom health check endpoint with different path to avoid conflicts
app.MapHealthChecks("/gateway/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            service = "ApiGateway",
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(result);
    }
});

// Add gateway info endpoint
app.MapGet("/gateway/info", () => new
{
    Service = "ApiGateway",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    Timestamp = DateTime.UtcNow,
    Services = new[]
    {
        new { Name = "AuthService", Url =  "https://authservice" },
        new { Name = "OrganizationService", Url = "https://organizationservice" },
        new { Name = "CampaignService", Url = "https://campaignservice" },
        new { Name = "DonationService", Url = "https://donationservice" }
    }
})
.WithName("GetGatewayInfo")
.WithOpenApi();

// Add a simple status endpoint
app.MapGet("/gateway/status", () =>
{
    return Results.Ok(new
    {
        Status = "Running",
        Timestamp = DateTime.UtcNow,
        Uptime = DateTime.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime(),
        Version = "1.0.0"
    });
})
.WithName("GetGatewayStatus")
.WithOpenApi();

app.Run();
