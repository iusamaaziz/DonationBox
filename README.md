# DonationBox - Microservices System

A comprehensive microservices-based donation management system built with .NET 8, ASP.NET Core Web API, Azure Functions, and YARP API Gateway.

## System Overview

DonationBox is designed as a scalable microservices architecture that allows organizations to manage donation campaigns, process donations, and track progress efficiently.

## Services

### 1. AuthService ✅ **COMPLETED**

**Location**: `src/Services/AuthService/`

A .NET 8 ASP.NET Core Web API service for user authentication with JWT tokens and Google OAuth support.

#### Features:
- ✅ **User Registration**: Create new user accounts with email and password
- ✅ **User Authentication**: Login with email and password
- ✅ **Google OAuth**: Sign in/up with Google accounts
- ✅ **JWT Tokens**: Secure access tokens with refresh token support
- ✅ **Token Validation**: Validate JWT tokens for other microservices via REST API and gRPC
- ✅ **SQL Server Database**: Entity Framework Core with SQL Server for data persistence
- ✅ **Password Security**: BCrypt password hashing
- ✅ **REST API**: Comprehensive REST API with Swagger documentation
- ✅ **Health Checks**: Built-in health check endpoints

#### API Endpoints:
- **Authentication**: Login, register, Google OAuth, token refresh/validation
- **Users**: User information retrieval
- **System**: Health checks and service information

#### Technology Stack:
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- Google OAuth
- gRPC Server
- BCrypt.Net
- Swagger/OpenAPI

### 2. DonationService ✅ **COMPLETED**

**Location**: `src/Services/DonationService/`

A .NET 8 ASP.NET Core Web API service for managing donation campaigns and donations with integrated authentication.

#### Features:
- ✅ **Campaign Management**: Create, update, delete, and manage donation campaigns
- ✅ **Donation Processing**: Handle donation pledges and payment processing
- ✅ **Authentication Integration**: Validates requests with AuthService via gRPC before processing
- ✅ **SQL Server Database**: Entity Framework Core with SQL Server for data persistence
- ✅ **Redis Caching**: Optional Redis caching controlled by `UseRedis` environment variable
  - Active campaigns cached for 15 minutes
  - Campaign statistics cached for 5 minutes
- ✅ **Event Publishing**: Publishes `DonationPledgedEvent` when donations are created
- ✅ **REST API**: Comprehensive REST API with Swagger documentation
- ✅ **Health Checks**: Built-in health check endpoints
- ✅ **Configuration**: Environment-based configuration with development and production settings

#### API Endpoints:
- **Campaigns**: CRUD operations, status management, statistics (create/update/delete require authentication)
- **Donations**: Create donations, process payments, retrieve by campaign (create requires authentication)
- **System**: Health checks and service information

#### Technology Stack:
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- Redis (optional)
- gRPC Client (for AuthService integration)
- Swagger/OpenAPI
- Health Checks

## Architecture

```
DonationBox/
├── src/
│   ├── Services/
│   │   ├── AuthService/              # ✅ Completed
│   │   │   ├── Controllers/          # Authentication API controllers
│   │   │   ├── Services/             # JWT & auth business logic
│   │   │   ├── Data/                 # EF Core DbContext
│   │   │   ├── Models/               # User & token entities
│   │   │   ├── DTOs/                 # Auth request/response models
│   │   │   └── Attributes/           # Custom authorization attributes
│   │   └── DonationService/          # ✅ Completed
│   │       ├── Controllers/          # REST API controllers
│   │       ├── Services/             # Business logic + auth validation
│   │       ├── Data/                 # EF Core DbContext
│   │       ├── Models/               # Domain entities
│   │       ├── DTOs/                 # Data transfer objects
│   │       ├── Events/               # Event models
│   │       ├── Attributes/           # Authorization attributes
│   │       └── Extensions/           # Controller extensions
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

### Running the Services

The system now consists of two microservices that work together. The DonationService depends on the AuthService for authentication.

#### Running AuthService (Required First)

1. **Navigate to the AuthService directory**:
   ```bash
   cd src/Services/AuthService
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
   - Navigate to `https://localhost:7002` or `http://localhost:5002`

#### Running DonationService

1. **Navigate to the DonationService directory** (in a new terminal):
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

#### Testing the Integration

1. **Register or login via AuthService**:
   ```bash
   curl -X POST https://localhost:7002/api/auth/login \
     -H "Content-Type: application/json" \
     -d '{"email": "john.doe@example.com", "password": "Password123!"}'
   ```

2. **Use the returned access token in DonationService**:
   ```bash
   curl -X POST https://localhost:5001/api/campaigns \
     -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
     -H "Content-Type: application/json" \
     -d '{"title": "New Campaign", "description": "Test campaign", "goalAmount": 10000}'
   ```

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

1. ✅ **AuthService** - User authentication with JWT and Google OAuth
2. ✅ **DonationService** - Campaign and donation management with authentication integration
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
