# DonationBox - Microservices System

A comprehensive microservices-based donation management system built with .NET 8, ASP.NET Core Web API, Azure Functions, and YARP API Gateway.

## System Overview

DonationBox is designed as a scalable microservices architecture that allows organizations to manage donation campaigns, process donations, and track progress efficiently.

## Services

### 1. DonationService ✅ **COMPLETED**

**Location**: `src/Services/DonationService/`

A .NET 8 ASP.NET Core Web API service for managing donation campaigns and donations.

#### Features:
- ✅ **Campaign Management**: Create, update, delete, and manage donation campaigns
- ✅ **Donation Processing**: Handle donation pledges and payment processing
- ✅ **SQL Server Database**: Entity Framework Core with SQL Server for data persistence
- ✅ **Redis Caching**: Optional Redis caching controlled by `UseRedis` environment variable
  - Active campaigns cached for 15 minutes
  - Campaign statistics cached for 5 minutes
- ✅ **Event Publishing**: Publishes `DonationPledgedEvent` when donations are created
- ✅ **REST API**: Comprehensive REST API with Swagger documentation
- ✅ **Health Checks**: Built-in health check endpoints
- ✅ **Configuration**: Environment-based configuration with development and production settings

#### API Endpoints:
- **Campaigns**: CRUD operations, status management, statistics
- **Donations**: Create donations, process payments, retrieve by campaign
- **System**: Health checks and service information

#### Technology Stack:
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Redis (optional)
- Swagger/OpenAPI
- Health Checks

## Architecture

```
DonationBox/
├── src/
│   ├── Services/
│   │   └── DonationService/          # ✅ Completed
│   │       ├── Controllers/          # REST API controllers
│   │       ├── Services/             # Business logic
│   │       ├── Data/                 # EF Core DbContext
│   │       ├── Models/               # Domain entities
│   │       ├── DTOs/                 # Data transfer objects
│   │       └── Events/               # Event models
│   ├── ApiGateway/                   # 🚧 Future: YARP API Gateway
│   └── Shared/                       # 🚧 Future: Shared libraries
└── DonationBox.sln
```

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- Redis (optional, for caching)
- Visual Studio 2022 or VS Code

### Running DonationService

1. **Navigate to the service directory**:
   ```bash
   cd src/Services/DonationService
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Run the service**:
   ```bash
   dotnet run
   ```

4. **Access Swagger UI**:
   - Navigate to `https://localhost:5001` or `http://localhost:5000`

### Configuration

The service uses environment-specific configuration files:

- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development settings (Redis enabled)
- `appsettings.Production.json`: Production settings template

Key configuration options:
- `UseRedis`: Enable/disable Redis caching
- `ConnectionStrings:DefaultConnection`: SQL Server connection
- `ConnectionStrings:Redis`: Redis connection string

## Database

The DonationService automatically creates and seeds the database on first run with sample campaigns and donations.

### Sample Data Includes:
- Community Center campaign (Active, $15k raised of $50k goal)
- Emergency Relief Fund (Active, $8.5k raised of $25k goal)
- School Technology Upgrade (Completed, $75k goal reached)

## Development Workflow

This project follows a microservices-first approach where each service is built independently:

1. ✅ **DonationService** - Campaign and donation management
2. 🚧 **UserService** - User authentication and management (Future)
3. 🚧 **PaymentService** - Payment processing integration (Future)
4. 🚧 **NotificationService** - Email and SMS notifications (Future)
5. 🚧 **YARP API Gateway** - Centralized API gateway (Future)

## Next Steps

The DonationService is complete and ready for use. Future development will include:

1. **UserService**: Authentication, authorization, and user management
2. **PaymentService**: Integration with payment processors (Stripe, PayPal)
3. **NotificationService**: Email and SMS notifications for donors and campaign creators
4. **API Gateway**: YARP-based gateway for routing and cross-cutting concerns
5. **Frontend Application**: React or Blazor frontend application
6. **Deployment**: Docker containers and Azure deployment scripts

## Contributing

Each service is self-contained and can be developed independently. Follow the established patterns in DonationService for consistency across the system.

## License

This project is part of a learning exercise in microservices architecture with .NET 8.
