# CampaignService

A microservice for managing donation campaigns in the DonationBox system.

## Overview

CampaignService handles all campaign-related operations including:
- Creating and managing donation campaigns
- Tracking campaign progress and statistics
- Managing campaign status and lifecycle
- Eventual consistency updates from donation payments

## Architecture

CampaignService follows a microservice architecture pattern with:
- **Event-Driven Communication**: Receives events from DonationService when payments are completed
- **Separate Database**: Isolated campaign data with its own database context
- **RESTful API**: Provides HTTP endpoints for campaign management
- **Authentication**: JWT token validation via AuthService

## Key Features

### Campaign Management
- Create, read, update, and delete donation campaigns
- Campaign status management (Draft, Active, Paused, Completed, Cancelled)
- Campaign statistics and progress tracking
- Multi-tenant support (campaigns by creator)

### Event Processing
- Consumes `DonationPaymentCompletedEvent` from DonationService
- Updates campaign amounts asynchronously
- Maintains eventual consistency between services

### Caching
- Redis-based caching for performance
- Configurable cache expiration
- Cache invalidation on data changes

### .NET Aspire Integration
- **Service Discovery**: Automatic discovery of AuthService for authentication
- **Database Provisioning**: SQL Server database `CampaignDb` automatically created
- **Observability**: OpenTelemetry tracing for campaign operations
- **Health Monitoring**: Real-time health status in Aspire dashboard
- **Event Processing**: Reliable event consumption with resilience patterns

## API Endpoints

### Campaigns
- `GET /api/campaigns` - Get all campaigns
- `GET /api/campaigns/active` - Get active campaigns
- `GET /api/campaigns/{id}` - Get campaign by ID
- `POST /api/campaigns` - Create new campaign
- `PUT /api/campaigns/{id}` - Update campaign
- `DELETE /api/campaigns/{id}` - Delete campaign

### Campaign Statistics
- `GET /api/campaigns/{id}/stats` - Get campaign statistics
- `POST /api/campaigns/{id}/refresh-stats` - Refresh campaign statistics

### Status Management
- `PATCH /api/campaigns/{id}/status` - Update campaign status

## Event Flow

```
DonationService → DonationPaymentCompletedEvent → CampaignService
    ↓                      ↓                           ↓
Process Payment    Publish Event              Update Campaign Amount
```

## Configuration

### With .NET Aspire

When running with Aspire, configuration is simplified:
- **Database**: `CampaignDb` (automatically provisioned)
- **AuthService**: Discovered automatically via service discovery
- **Redis**: Configured via Aspire resources (optional)
- **Service Discovery**: HTTP client configured with resilience patterns

### Standalone Configuration (Legacy)

### Environment Variables
- `UseRedis`: Enable/disable Redis caching (default: false)
- `AuthService:Url`: URL for AuthService gRPC endpoint

### Database
- Uses SQL Server with Entity Framework Core
- Database: `CampaignServiceDb`
- Auto-creates database and applies migrations

## Dependencies

- **AuthService**: JWT token validation
- **DonationService**: Source of donation events
- **Redis**: Optional caching (when enabled)
- **SQL Server**: Primary data store

## Health Checks

- `GET /health` - Service health status
- `GET /info` - Service information and configuration

## Development

### Running with .NET Aspire (Recommended)

The CampaignService is integrated with .NET Aspire for simplified development:

1. **Start the Aspire AppHost**:
   ```bash
   cd src/AspireHost
   dotnet run
   ```

2. **Automatic Setup**:
   - SQL Server database `CampaignDb` is provisioned
   - AuthService dependency is automatically discovered
   - Redis caching is configured if available
   - OpenTelemetry tracing is enabled

3. **Development Experience**:
   - Hot reload for code changes
   - Real-time logs in Aspire dashboard
   - Service health monitoring
   - Database migrations applied automatically

### Running Standalone (Legacy Method)

### Prerequisites
- .NET 8.0 SDK
- SQL Server (LocalDB or full instance)
- Redis (optional, for caching)

### Running Locally
```bash
# Restore packages
dotnet restore

# Run migrations (if needed)
dotnet ef database update

# Run the service
dotnet run
```

### Testing
```bash
# Run unit tests
dotnet test

# Run integration tests
dotnet test --filter Category=Integration
```


## Monitoring

### With .NET Aspire
- **Structured logging** with correlation IDs via OpenTelemetry
- **Health checks** for all dependencies (database, AuthService, Redis)
- **Performance metrics** collected automatically via OpenTelemetry
- **Distributed tracing** for campaign operations and event processing
- **Real-time dashboard** showing service health and metrics

### Standalone Monitoring (Legacy)
- Structured logging with Serilog
- Health checks for dependencies
- Performance metrics via built-in ASP.NET Core metrics
- Distributed tracing support (can be added)

## Security

- JWT token authentication for protected endpoints
- Input validation and sanitization
- CORS configuration for cross-origin requests
- HTTPS enforcement in production

## Future Enhancements

- [ ] Message queue integration (RabbitMQ, Azure Service Bus)
- [ ] Advanced caching strategies
- [ ] Campaign analytics and reporting
- [ ] Campaign templates and cloning
- [ ] Integration with payment processors
- [ ] Real-time campaign updates via WebSockets
