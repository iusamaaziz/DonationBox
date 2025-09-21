# DonationService

A .NET 8 microservice for managing donation campaigns and donations in the DonationBox system.

## Features

- **Campaign Management**: Create, update, and manage donation campaigns with goals, deadlines, and status tracking
- **Donation Processing**: Handle donation pledges and payment processing
- **Redis Caching**: Optional Redis caching for campaign statistics and active campaigns (controlled by `UseRedis` environment variable)
- **Event Publishing**: Publishes events when donations are pledged for integration with other microservices
- **REST API**: Comprehensive REST API with Swagger documentation
- **Entity Framework Core**: SQL Server database with EF Core for data persistence
- **Health Checks**: Built-in health checks for database and Redis connectivity
- **Service Discovery**: Automatic discovery of AuthService for authentication
- **Observability**: OpenTelemetry integration for donation processing flows
- **Database Provisioning**: SQL Server database `DonationDb` automatically created

## API Endpoints

### Campaigns
- `GET /api/campaigns` - Get all campaigns
- `GET /api/campaigns/active` - Get active campaigns (cached)
- `GET /api/campaigns/{id}` - Get campaign by ID
- `GET /api/campaigns/{id}/stats` - Get campaign statistics (cached)
- `POST /api/campaigns` - Create new campaign
- `PUT /api/campaigns/{id}` - Update campaign
- `DELETE /api/campaigns/{id}` - Delete campaign
- `PATCH /api/campaigns/{id}/status` - Update campaign status
- `GET /api/campaigns/creator/{createdBy}` - Get campaigns by creator
- `POST /api/campaigns/{id}/refresh-stats` - Refresh campaign statistics

### Donations
- `POST /api/donations` - Create donation pledge
- `GET /api/donations/{id}` - Get donation by ID
- `GET /api/donations/transaction/{transactionId}` - Get donation by transaction ID
- `GET /api/donations/campaign/{campaignId}` - Get donations for campaign
- `POST /api/donations/{id}/process` - Process donation payment

### System
- `GET /health` - Health check endpoint
- `GET /info` - Service information

## Configuration

### With .NET Aspire

When running with Aspire, configuration is simplified:
- **Database**: `DonationDb` (automatically provisioned by Aspire)
- **AuthService**: Discovered automatically via service discovery
- **Redis**: Configured via Aspire resources when available
- **Event Publishing**: Reliable event delivery with resilience patterns

### Standalone Configuration (Legacy)

### Environment Variables

- `UseRedis`: Boolean flag to enable/disable Redis caching (default: false)

### Connection Strings

- `DefaultConnection`: SQL Server connection string
- `Redis`: Redis connection string (only used when UseRedis=true)

### Example Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DonationServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    "Redis": "localhost:6379"
  },
  "UseRedis": true
}
```

## Running the Service

### Running with .NET Aspire (Recommended)

The DonationService is integrated with .NET Aspire for simplified development:

1. **Start the Aspire AppHost**:
   ```bash
   cd src/AspireHost
   dotnet run
   ```

2. **Automatic Setup**:
   - SQL Server database `DonationDb` is provisioned
   - AuthService dependency is automatically discovered
   - Redis caching is configured if available
   - Event publishing is set up with resilience patterns

3. **Development Experience**:
   - Hot reload for code changes
   - Real-time logs in Aspire dashboard
   - Service health monitoring
   - Database migrations applied automatically

### Running Standalone (Legacy Method)

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- Redis (optional, for caching)

### Development

1. **Clone and navigate to the project**:
   ```bash
   cd src/Services/DonationService
   ```

2. **Restore packages**:
   ```bash
   dotnet restore
   ```

3. **Run the service**:
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**:
   - Navigate to `https://localhost:5001` or `http://localhost:5000`

### Database

The service uses Entity Framework Core with SQL Server. The database is automatically created and seeded with sample data on first run.

### Redis Caching

When `UseRedis=true`:
- Active campaigns are cached for 15 minutes
- Campaign statistics are cached for 5 minutes
- Cache is automatically invalidated when campaigns or donations are modified

## Data Models

### DonationCampaign
- Campaign details (title, description, goal, dates)
- Status tracking (Draft, Active, Paused, Completed, Cancelled)
- Progress calculations
- Relationship to donations

### Donation
- Donation details (amount, donor information)
- Payment status tracking
- Transaction ID for payment processing
- Relationship to campaign

## Events

The service publishes the following events:

### DonationPledgedEvent
Published when a new donation is created, containing:
- Donation details
- Campaign information
- Progress statistics

## Architecture

- **Controllers**: REST API endpoints
- **Services**: Business logic layer
- **Data**: Entity Framework DbContext and models
- **DTOs**: Data transfer objects for API requests/responses
- **Events**: Event models for pub/sub messaging

## Technology Stack

- .NET 8
- ASP.NET Core Web API
- .NET Aspire (orchestration, service discovery, observability)
- Entity Framework Core
- SQL Server
- Redis (optional)
- gRPC Client (for AuthService integration)
- OpenTelemetry for distributed tracing
- Swagger/OpenAPI
- Health Checks

## Development Notes

- The service follows Clean Architecture principles
- All API endpoints include proper error handling and logging
- Model validation is implemented using Data Annotations
- The service is designed for containerization and cloud deployment
