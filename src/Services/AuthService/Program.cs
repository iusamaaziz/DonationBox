using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using AuthService.Data;
using AuthService.Services;

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
    ?? "Server=(localdb)\\mssqllocaldb;Database=AuthServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true";

builder.Services.AddDbContext<AuthDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? "your-super-secret-jwt-key-that-is-at-least-256-bits-long!";
var issuer = jwtSettings["Issuer"] ?? "AuthService";
var audience = jwtSettings["Audience"] ?? "DonationBoxClients";

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
});

// Configure Google Authentication (for OAuth validation)
var googleClientId = builder.Configuration["GoogleAuth:ClientId"];
if (!string.IsNullOrEmpty(googleClientId))
{
    builder.Services.AddAuthentication()
        .AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = builder.Configuration["GoogleAuth:ClientSecret"] ?? "";
        });
}

builder.Logging.AddConsole();
builder.Services.AddLogging(logging =>
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Information);
});

// Register application services
builder.Services.AddScoped<IAuthService, AuthServiceImpl>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<ITokenValidationService, TokenValidationService>();

// Add gRPC services
builder.Services.AddGrpc();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Authentication Service API",
        Version = "v1",
        Description = "A microservice for user authentication with JWT and Google OAuth support",
        Contact = new OpenApiContact
        {
            Name = "DonationBox Team",
            Email = "support@donationbox.com"
        }
    });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
                }
            },
            Array.Empty<string>()
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
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuthDbContext>("database")
    .AddSqlServer(connectionString, name: "sqlserver");

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Authentication Service API v1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at the app's root
    });
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map gRPC services
app.MapGrpcService<GrpcAuthenticationService>();

// Add health check endpoint
app.MapHealthChecks("/health");

// Add a simple info endpoint
app.MapGet("/info", () => new
{
    Service = "AuthService",
    Version = "1.0.0",
    Environment = app.Environment.EnvironmentName,
    JwtIssuer = issuer,
    JwtAudience = audience,
    GoogleAuthEnabled = !string.IsNullOrEmpty(googleClientId),
    Timestamp = DateTime.UtcNow
})
.WithName("GetServiceInfo")
.WithOpenApi();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.Run();
