# DonorService

A microservice for managing donors and their welfare organizations in the DonationBox system.

## Overview

The DonorService is responsible for:
- Managing donor profiles and information
- Creating and managing welfare organizations
- Validating organization ownership and access
- Providing organization data to other services via gRPC

## Architecture

The service follows a microservice architecture pattern with:
- **gRPC** for inter-service communication
- **Entity Framework Core** for data persistence
- **REST API** for external HTTP clients
- **SQL Server** as the database
- **Service Discovery** via .NET Aspire for automatic AuthService discovery
- **Observability** with OpenTelemetry for distributed tracing
- **Database Provisioning** with SQL Server `DonorDb` automatically created

## Service Endpoints

### HTTP Endpoints (Port 5002/5003)
- `GET /swagger` - API documentation
- `GET /health` - Health check endpoint
- `GET /info` - Service information

### gRPC Endpoints (Port 5004)
- `DonorServiceGrpc.DonorServiceGrpc` - Main service interface

## Dependencies

### External Services
- **AuthService** (gRPC) - User authentication and validation
  - URL: `http://localhost:5001` (configurable)

### Database
- **SQL Server** with Entity Framework Core
- Connection string: Configurable in `appsettings.json`

## Configuration

### With .NET Aspire

When running with Aspire, configuration is simplified:
- **Database**: `DonorDb` (automatically provisioned)
- **AuthService**: Discovered automatically via service discovery
- **gRPC Endpoints**: Registered automatically with service discovery
- **Health Monitoring**: Built-in health checks visible in Aspire dashboard

### Standalone Configuration (Legacy)

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=DonorServiceDb;..."
  },
  "AuthService": {
    "Url": "http://localhost:5001"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5002"
      },
      "Https": {
        "Url": "https://localhost:5003"
      },
      "gRPC": {
        "Url": "http://localhost:5004",
        "Protocols": "Http2"
      }
    }
  }
}
```

## API Usage

### gRPC Methods

#### Donor Management
```protobuf
// Get donor profile
rpc GetDonor(GetDonorRequest) returns (GetDonorResponse);

// Create or update donor profile
rpc CreateDonor(CreateDonorRequest) returns (CreateDonorResponse);

// Get donor's organizations
rpc GetDonorOrganizations(GetDonorOrganizationsRequest) returns (GetDonorOrganizationsResponse);
```

#### Organization Management
```protobuf
// Create organization
rpc CreateOrganization(CreateOrganizationRequest) returns (CreateOrganizationResponse);

// Get organization details
rpc GetOrganization(GetOrganizationRequest) returns (GetOrganizationResponse);

// Update organization
rpc UpdateOrganization(UpdateOrganizationRequest) returns (UpdateOrganizationResponse);

// Get all organizations
rpc GetAllOrganizations(GetAllOrganizationsRequest) returns (GetAllOrganizationsResponse);

// Validate organization access
rpc ValidateOrganizationAccess(ValidateOrganizationAccessRequest) returns (ValidateOrganizationAccessResponse);
```

## Data Models

### Donor
- `UserId` - Unique identifier (matches AuthService User ID)
- `Bio` - Donor biography
- `Interests` - JSON array of donation interests
- `PhoneNumber` - Contact phone number
- `Address` - Physical address
- `IsActive` - Account status

### WelfareOrganization
- `Id` - Unique organization identifier
- `Name` - Organization name
- `Description` - Organization description
- `Type` - Organization type (Charity, Foundation, etc.)
- `Mission` - Organization mission statement
- `WebsiteUrl` - Organization website
- `ContactEmail` - Contact email
- `ContactPhone` - Contact phone
- `Address` - Organization address
- `TaxId` - Tax identification number
- `CreatedByUserId` - Creator's user ID
- `IsVerified` - Verification status
- `IsActive` - Organization status

## Running the Service

### Running with .NET Aspire (Recommended)

The DonorService is integrated with .NET Aspire for simplified development:

1. **Start the Aspire AppHost**:
   ```bash
   cd src/AspireHost
   dotnet run
   ```

2. **Automatic Setup**:
   - SQL Server database `DonorDb` is provisioned
   - AuthService dependency is automatically discovered
   - gRPC endpoints are registered with service discovery
   - Health monitoring is enabled

3. **Development Experience**:
   - Hot reload for code changes
   - Real-time logs in Aspire dashboard
   - Service health monitoring
   - Database migrations applied automatically

### Running Standalone (Legacy Method)

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- AuthService running (for user validation)

### Development

1. **Navigate to the service directory**:
   ```bash
   cd src/Services/DonorService
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
   - Navigate to `https://localhost:5003` or `http://localhost:5002`

### Production

```bash
# Build for production
dotnet publish -c Release

# Run the published application
dotnet DonorService.dll
```

### Database

The service uses Entity Framework Core with SQL Server. The database is automatically created and seeded with sample data on first run.

## Database Migrations

The service automatically handles database initialization and migrations on startup.

### Manual Migration (if needed)
```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

## Testing

### Health Checks
```bash
# Check service health
curl http://localhost:5002/health

# Get service information
curl http://localhost:5002/info
```

### gRPC Testing
Use tools like `grpcui` or `grpcurl` for testing gRPC endpoints:

```bash
# Install grpcurl
# Then test endpoints
grpcurl -plaintext localhost:5004 list
grpcurl -plaintext localhost:5004 donor.DonorService/GetDonor
```

## Integration with Other Services

### DonationService Integration
The DonationService uses DonorService to:
- Validate organization ownership for campaigns
- Get organization details for campaign display
- Verify user permissions for organization-based operations

### AuthService Integration
The DonorService uses AuthService to:
- Validate user tokens and permissions
- Get user information for donor profile creation
- Verify user existence before creating donor profiles

## Error Handling

The service implements comprehensive error handling:
- gRPC status codes for different error types
- Structured logging for debugging
- Graceful degradation for external service failures
- Input validation with detailed error messages

## Monitoring

### Logging
- Structured logging with Microsoft.Extensions.Logging
- Different log levels (Debug, Information, Warning, Error)
- Request/response logging for debugging

### Health Checks
- Database connectivity checks
- External service dependency checks
- Application health status

## Security

### Authentication
- JWT token validation via AuthService
- User permission checks for organization operations
- Secure gRPC communication channels

### Authorization
- Organization ownership validation
- User role-based access control
- API endpoint protection

## Technology Stack

- .NET 8
- ASP.NET Core Web API
- .NET Aspire (orchestration, service discovery, observability)
- Entity Framework Core
- SQL Server
- gRPC Server & Client
- OpenTelemetry for distributed tracing
- Swagger/OpenAPI
- Health Checks

## Development Guidelines

### Code Structure
```
DonorService/
├── Controllers/     # REST API controllers
├── Models/         # Entity models
├── DTOs/          # Data transfer objects
├── Services/      # Business logic services
├── Data/          # Database context and migrations
├── Protos/        # gRPC protocol definitions
└── Extensions/    # Extension methods
```

### Naming Conventions
- PascalCase for classes and methods
- camelCase for properties and parameters
- Descriptive names for all entities
- Consistent async method naming

### Error Handling
- Use custom exceptions for business logic errors
- Return appropriate gRPC status codes
- Log errors with sufficient context
- Provide meaningful error messages to clients
