# DonationBox - Microservices System

A comprehensive microservices-based donation management system built with .NET 8, ASP.NET Core Web API, Azure Functions, and YARP API Gateway.

## System Overview

DonationBox is designed as a scalable microservices architecture that allows organizations to manage donation campaigns, process donations, and track progress efficiently. The system consists of five core services working together to provide a complete donation management solution.

## Services

### 1. AuthService

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

### 2. DonationService

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

### 3. DonorService

**Location**: `src/Services/DonorService/`

A .NET 8 microservice for managing donors and their welfare organizations in the DonationBox system.

#### Features:
- ✅ **Donor Profile Management**: Create and manage donor profiles with detailed information
- ✅ **Organization Management**: Create and manage welfare organizations
- ✅ **Organization Ownership**: Validate organization ownership and access permissions
- ✅ **gRPC Integration**: High-performance gRPC service for inter-service communication
- ✅ **REST API**: HTTP endpoints for external client integration
- ✅ **SQL Server Database**: Entity Framework Core with SQL Server for data persistence
- ✅ **Authentication Integration**: Validates users with AuthService via gRPC
- ✅ **Health Checks**: Built-in health check endpoints

#### API Endpoints:
- **HTTP REST API**: Donor and organization management
- **gRPC Services**: High-performance inter-service communication
- **System**: Health checks and service information

#### Technology Stack:
- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- gRPC Server & Client
- Swagger/OpenAPI

### 4. PaymentService

**Location**: `src/Services/PaymentService/`

An Azure Functions-based microservice for processing donation payments with Durable Functions Saga orchestration, distributed locking, and reliable event delivery patterns.

#### Features:
- ✅ **Saga Orchestration**: Durable Functions-based Saga pattern for reliable payment processing
- ✅ **Distributed Locking**: Redis-based distributed locks to prevent duplicate payments
- ✅ **Outbox Pattern**: Reliable event delivery with automatic retry and exponential backoff
- ✅ **Payment Ledger**: Complete audit trail of all payment transactions and operations
- ✅ **Multiple Payment Gateways**: Support for various payment methods (Credit Card, PayPal, Bank Transfer, etc.)
- ✅ **Azure Functions v4**: Modern isolated worker runtime with .NET 8
- ✅ **Health Monitoring**: Built-in health checks and monitoring capabilities

#### API Endpoints:
- **Payments**: Process payments, get status, refunds, and payment details
- **Admin**: Administrative operations and system information
- **System**: Health checks and service information

#### Technology Stack:
- .NET 8
- Azure Functions v4 (Isolated Worker)
- Durable Functions
- Entity Framework Core
- SQL Server
- Redis (optional)
- Azure Storage
- Swagger/OpenAPI

### 5. ApiGateway

**Location**: `src/ApiGateway/`

A YARP-based API gateway that provides centralized routing, cross-cutting concerns, and unified API access to the microservices system.

#### Features:
- ✅ **Reverse Proxy**: YARP-based routing to all microservices with intelligent path transformations
- ✅ **Authentication**: JWT token validation and forwarding to downstream services
- ✅ **Load Balancing**: Built-in load balancing support for service instances
- ✅ **Health Monitoring**: Comprehensive health checks for all services with automatic failover
- ✅ **CORS Support**: Configurable cross-origin resource sharing for frontend applications
- ✅ **Request Logging**: Detailed request/response logging with correlation tracking
- ✅ **Swagger Integration**: Centralized API documentation for all services
- ✅ **Service Discovery**: Automatic service discovery and routing configuration

#### API Endpoints:
- **Gateway Routes**: `/api/auth/*`, `/api/donations/*`, `/api/campaigns/*`, `/api/donors/*`, `/api/payment/*`
- **Gateway Services**: `/gateway/info`, `/gateway/status` (gateway information and status)
- **Health Monitoring**: `/health` (comprehensive health check endpoint)
- **API Documentation**: `/swagger` (centralized Swagger UI for all services)

#### Technology Stack:
- .NET 8
- YARP (Yet Another Reverse Proxy)
- ASP.NET Core Web API
- JWT Bearer Authentication
- Swagger/OpenAPI
- Health Checks

## Architecture

### Service Communication Flow

```
┌─────────────┐    ┌─────────────┐    ┌─────────────┐
│   Clients   │────│  ApiGateway │────│  AuthService│
│             │    │   (YARP)    │    │   (JWT)     │
└─────────────┘    └─────────────┘    └─────────────┘
                        │                   │
                        │                   │
                        ▼                   ▼
               ┌─────────────┐    ┌─────────────┐
               │DonationSvc  │────│ DonorService │
               │             │    │             │
               └─────────────┘    └─────────────┘
                        │
                        │
                        ▼
               ┌─────────────┐
               │PaymentSvc   │
               │ (Azure Func)│
               └─────────────┘
```

### Project Structure

```
DonationBox/
├── src/
│   ├── Services/
│   │   ├── AuthService/              # User authentication & JWT
│   │   │   ├── Controllers/          # Authentication API controllers
│   │   │   ├── Services/             # JWT & auth business logic
│   │   │   ├── Data/                 # EF Core DbContext
│   │   │   ├── Models/               # User & token entities
│   │   │   ├── DTOs/                 # Auth request/response models
│   │   │   └── Attributes/           # Custom authorization attributes
│   │   ├── DonationService/          # Campaign & donation management
│   │   │   ├── Controllers/          # REST API controllers
│   │   │   ├── Services/             # Business logic + auth validation
│   │   │   ├── Data/                 # EF Core DbContext
│   │   │   ├── Models/               # Domain entities
│   │   │   ├── DTOs/                 # Data transfer objects
│   │   │   ├── Events/               # Event models
│   │   │   ├── Attributes/           # Authorization attributes
│   │   │   └── Extensions/           # Controller extensions
│   │   ├── DonorService/             # Donor & organization management
│   │   │   ├── Controllers/          # REST API controllers
│   │   │   ├── Services/             # Business logic
│   │   │   ├── Data/                 # EF Core DbContext
│   │   │   ├── Models/               # Donor & organization entities
│   │   │   ├── DTOs/                 # Data transfer objects
│   │   │   ├── Protos/               # gRPC protocol definitions
│   │   │   └── Extensions/           # Extension methods
│   │   └── PaymentService/           # Payment processing (Azure Functions)
│   │       ├── Activities/           # Saga orchestration activities
│   │       ├── Functions/            # HTTP triggers
│   │       ├── Orchestrations/       # Durable Functions orchestrators
│   │       ├── Data/                 # EF Core DbContext
│   │       ├── Models/               # Payment entities
│   │       ├── DTOs/                 # Payment request/response models
│   │       └── Services/             # Business logic services
│   ├── ApiGateway/                   # YARP API Gateway (Complete)
│   └── Shared/                       # 🚧 Future: Shared libraries
└── DonationBox.sln
```

### Technology Stack

- **Backend**: .NET 8, ASP.NET Core Web API
- **Database**: SQL Server with Entity Framework Core
- **Caching**: Redis (optional)
- **Functions**: Azure Functions v4 (Isolated Worker)
- **API Gateway**: YARP (Yet Another Reverse Proxy)
- **Communication**: REST APIs, gRPC for inter-service communication
- **Authentication**: JWT tokens with refresh token support
- **Documentation**: Swagger/OpenAPI
- **Monitoring**: Health checks and structured logging

## Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- Azure Functions Core Tools v4 (for PaymentService)
- Azure Storage Emulator or Azurite (for Durable Functions)
- Redis (optional, for caching and distributed locking)
- Visual Studio 2022 or VS Code

### Service Dependencies

The services have the following startup order and dependencies:

1. **AuthService** - No dependencies (start first)
2. **DonorService** - Depends on AuthService (gRPC)
3. **DonationService** - Depends on AuthService (gRPC)
4. **PaymentService** - Depends on DonationService (events)
5. **ApiGateway** - Depends on all services (routes to them)

### Running the Services

#### 1. Start AuthService (Foundation Service)

```bash
cd src/Services/AuthService
dotnet restore
dotnet run
```

- **Swagger UI**: `https://localhost:7002` or `http://localhost:5002`
- **gRPC Endpoint**: `http://localhost:5001` (for inter-service communication)

#### 2. Start DonorService

```bash
cd src/Services/DonorService
dotnet restore
dotnet run
```

- **HTTP API**: `https://localhost:5003` or `http://localhost:5002`
- **gRPC Endpoint**: `http://localhost:5004`

#### 3. Start DonationService

```bash
cd src/Services/DonationService
dotnet restore
dotnet run
```

- **Swagger UI**: `https://localhost:5001` or `http://localhost:5000`
- **Depends on**: AuthService (gRPC for authentication)

#### 4. Start PaymentService (Azure Functions)

```bash
# Install Azure Functions Core Tools (if not already installed)
npm install -g azure-functions-core-tools@4 --unsafe-perm true

# Start Azure Storage Emulator (required for Durable Functions)
# Option 1: Azurite
npm install -g azurite
azurite --silent --location c:\azurite --debug c:\azurite\debug.log

# Option 2: Azure Storage Emulator (Windows only)
# Start from Azure Storage Emulator UI

cd src/Services/PaymentService
func start
```

- **Functions Host**: `http://localhost:7071`
- **Swagger UI**: `http://localhost:7071/api/swagger/ui`

#### 5. Start ApiGateway

```bash
cd src/ApiGateway/ApiGateway
dotnet restore
dotnet run
```

- **Gateway URL**: `http://localhost:5000` (HTTP) or `https://localhost:7000` (HTTPS)
- **Swagger UI**: `http://localhost:5000/swagger`
- **Health Check**: `http://localhost:5000/health`
- **Routes to**: All other services via YARP reverse proxy

### Testing the System

#### 1. Register a User (AuthService)

```bash
curl -X POST https://localhost:7002/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

#### 2. Login to Get JWT Token

```bash
curl -X POST https://localhost:7002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "Password123!"
  }'
```

Save the returned `accessToken` for use in other services.

#### 3. Create a Donor Profile (DonorService)

```bash
curl -X POST https://localhost:5003/api/donors \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "bio": "Passionate about helping communities",
    "phoneNumber": "+1234567890",
    "address": "123 Main St, City, State"
  }'
```

#### 4. Create a Campaign (DonationService)

```bash
curl -X POST https://localhost:5001/api/campaigns \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Community Center Renovation",
    "description": "Help us renovate the local community center",
    "goalAmount": 50000.00,
    "targetDate": "2024-12-31T00:00:00Z"
  }'
```

#### 5. Process a Payment (PaymentService)

```bash
curl -X POST http://localhost:7071/api/payments/process \
  -H "Content-Type: application/json" \
  -d '{
    "donationId": 1,
    "campaignId": 1,
    "amount": 100.00,
    "currency": "USD",
    "donorName": "John Doe",
    "donorEmail": "john.doe@example.com",
    "paymentMethod": "CreditCard",
    "paymentDetails": {
      "cardNumber": "4242424242424242",
      "expiryMonth": 12,
      "expiryYear": 2025,
      "cvv": "123"
    }
  }'
```

### Testing via ApiGateway

Once all services are running, you can also test the entire system through the ApiGateway:

#### Gateway Health Check
```bash
curl http://localhost:5000/health
```

#### Gateway Information
```bash
curl http://localhost:5000/gateway/info
```

#### Register User via Gateway
```bash
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

#### Get Campaigns via Gateway
```bash
curl http://localhost:5000/api/campaigns
```

#### Process Payment via Gateway
```bash
curl -X POST http://localhost:5000/api/payment/process \
  -H "Content-Type: application/json" \
  -d '{
    "donationId": 1,
    "campaignId": 1,
    "amount": 100.00
  }'
```

### Configuration

Each service uses environment-specific configuration files:

- `appsettings.json`: Base configuration
- `appsettings.Development.json`: Development settings
- `appsettings.Production.json`: Production settings template

#### Key Configuration Options:

**AuthService:**
- `JwtSettings:SecretKey`: JWT signing key (must be 256+ bits)
- `ConnectionStrings:DefaultConnection`: SQL Server connection
- `GoogleAuth:ClientId/ClientSecret`: Google OAuth credentials

**DonationService:**
- `UseRedis`: Enable/disable Redis caching (default: false)
- `ConnectionStrings:DefaultConnection`: SQL Server connection
- `ConnectionStrings:Redis`: Redis connection string

**DonorService:**
- `ConnectionStrings:DefaultConnection`: SQL Server connection
- `AuthService:Url`: AuthService gRPC endpoint

**PaymentService (local.settings.json):**
- `AzureWebJobsStorage`: Azure Storage connection
- `ConnectionStrings:DefaultConnection`: SQL Server connection
- `ConnectionStrings:Redis`: Redis connection (optional)

**ApiGateway:**
- `ReverseProxy:Routes`: YARP route configuration for all services
- `ReverseProxy:Clusters`: Service cluster definitions with health checks
- `JwtSettings:SecretKey`: JWT validation key for gateway authentication
- `CorsSettings:AllowedOrigins`: Configurable CORS origins for frontend apps

### Sample Data

The services automatically create and seed databases with sample data:

**AuthService Sample Users:**
- `admin@donationbox.com` / `Admin123!`
- `john.doe@example.com` / `Password123!`
- `jane.smith@example.com` / `Password123!`

**DonationService Sample Campaigns:**
- Community Center Renovation (Active, $15k raised of $50k goal)
- Emergency Relief Fund (Active, $8.5k raised of $25k goal)
- School Technology Upgrade (Completed, $75k goal reached)

## Development Workflow

This project follows a microservices-first approach where each service is built independently and deployed separately:

### Current Services Status:

1. ✅ **AuthService** - Complete user authentication with JWT and Google OAuth
2. ✅ **DonationService** - Complete campaign and donation management with authentication integration
3. ✅ **DonorService** - Complete donor and organization management
4. ✅ **PaymentService** - Complete payment processing with Saga orchestration
5. ✅ **ApiGateway** - Complete YARP-based API gateway with routing, authentication, and monitoring

### Service Development Guidelines:

- **Independent Development**: Each service can be developed and tested independently
- **Contract-First Design**: Define API contracts (REST/gRPC) before implementation
- **Database Isolation**: Each service maintains its own database schema
- **Event-Driven Communication**: Services communicate via events where appropriate
- **Health Monitoring**: All services include health checks and monitoring endpoints

### Testing Strategy:

- **Unit Tests**: Individual components and business logic
- **Integration Tests**: Service-to-service communication
- **Contract Tests**: API contract validation
- **End-to-End Tests**: Complete user workflows across services

## Next Steps

The core DonationBox microservices system is now complete and fully functional! All five services (AuthService, DonationService, DonorService, PaymentService, and ApiGateway) are implemented and ready for production use.

Future development will focus on enhancing the system with additional capabilities:

### 1. **Enhanced ApiGateway Features**
- Implement rate limiting and request throttling
- Add request/response transformation middleware
- Implement circuit breaker patterns
- Add distributed tracing with OpenTelemetry

### 2. **NotificationService** (New Service)
- Email notifications for donors and campaign creators
- SMS notifications for payment confirmations
- Push notifications for mobile applications
- Template-based notification system

### 3. **ReportingService** (New Service)
- Campaign performance analytics
- Donor contribution tracking
- Payment processing reports
- Real-time dashboards and metrics

### 4. **Frontend Applications**
- **Web Application**: React-based admin and donor portals
- **Mobile Applications**: React Native or Flutter mobile apps
- **Progressive Web App**: PWA for offline functionality

### 5. **Infrastructure & DevOps**
- **Docker Containerization**: Multi-stage Docker builds for all services
- **Kubernetes Orchestration**: K8s manifests for production deployment
- **CI/CD Pipelines**: GitHub Actions or Azure DevOps pipelines
- **Monitoring & Observability**: Application Insights, Prometheus, Grafana
- **Security**: API security scanning, vulnerability assessments

### 6. **Advanced Features**
- **Multi-tenancy**: Support for multiple organizations
- **Internationalization**: Multi-language support
- **Payment Integration**: Additional payment gateways (Stripe, PayPal)
- **Advanced Analytics**: Machine learning for donation prediction
- **Blockchain Integration**: Transparent donation tracking

## Contributing

### Development Guidelines

Each service is self-contained and can be developed independently. However, to maintain consistency across the system, follow these established patterns:

#### Code Structure
- **Controllers**: REST API endpoints with proper error handling
- **Services**: Business logic layer with dependency injection
- **Data**: Entity Framework DbContext and repository patterns
- **Models**: Domain entities with data annotations
- **DTOs**: Data transfer objects for API requests/responses
- **Protos**: gRPC protocol definitions (where applicable)

#### Naming Conventions
- PascalCase for classes, methods, and properties
- camelCase for parameters and local variables
- Descriptive names that clearly indicate purpose
- Consistent async method naming with "Async" suffix

#### Error Handling
- Use custom exceptions for business logic errors
- Return appropriate HTTP status codes
- Include detailed error messages for debugging
- Implement proper logging with correlation IDs

#### Testing
- Unit tests for business logic
- Integration tests for API endpoints
- Mock external dependencies
- Maintain high test coverage (>80%)

#### Documentation
- Update README files when adding new features
- Document API endpoints with Swagger annotations
- Include code comments for complex business logic
- Update this main README when adding new services

### Service-Specific Guidelines

#### AuthService
- JWT tokens must be validated by other services
- Maintain backward compatibility with existing tokens
- Secure password hashing with BCrypt
- Implement proper refresh token rotation

#### DonationService
- Validate all requests with AuthService
- Use Redis caching for performance optimization
- Publish events for payment processing
- Maintain data consistency across campaigns and donations

#### DonorService
- Validate user existence with AuthService
- Implement proper organization ownership validation
- Use gRPC for high-performance inter-service communication
- Maintain referential integrity with other services

#### PaymentService
- Implement Saga pattern for reliable payment processing
- Use distributed locks to prevent duplicate payments
- Implement outbox pattern for event publishing
- Maintain comprehensive payment audit trails

#### ApiGateway
- Configure YARP routes for new services when added
- Implement proper health checks for all downstream services
- Maintain JWT token validation and forwarding
- Configure CORS policies for frontend applications
- Implement comprehensive logging and monitoring

### Pull Request Process

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feature/amazing-feature`)
3. **Commit** your changes (`git commit -m 'Add amazing feature'`)
4. **Push** to the branch (`git push origin feature/amazing-feature`)
5. **Open** a Pull Request with detailed description
6. **Wait** for review and approval

### Issue Reporting

When reporting issues:
- Use descriptive titles
- Include steps to reproduce
- Attach relevant logs and error messages
- Specify the service and environment affected
- Suggest potential solutions if possible

## License

This project is part of a learning exercise in microservices architecture with .NET 8. It demonstrates modern software development practices including:

- Domain-Driven Design (DDD)
- Command Query Responsibility Segregation (CQRS)
- Event Sourcing
- Saga Orchestration
- Microservices Communication Patterns
- Containerization and Orchestration
- Cloud-Native Development

## Acknowledgments

- Microsoft for .NET 8 and ASP.NET Core
- The open-source community for the amazing tools and libraries
- Contributors who help improve and extend this project

---

**DonationBox** - Building the future of charitable giving through technology.
