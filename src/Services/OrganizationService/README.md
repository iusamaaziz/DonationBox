# OrganizationService

The OrganizationService is a microservice responsible for managing charity organizations in the DonationBox platform. It provides CRUD operations for organizations with proper authentication and authorization.

## Features

- **CRUD Operations**: Create, Read, Update, and Delete organizations
- **Authentication**: Uses gRPC communication with AuthService for token validation
- **Authorization**: Users can only modify organizations they created
- **Public Access**: Organization listing and details are publicly accessible
- **Database**: Uses Entity Framework Core with SQL Server
- **Clean Architecture**: Separated into Models, DTOs, Services, and Controllers

## API Endpoints

### Public Endpoints (No Authentication Required)

- `GET /api/organizations` - Get all active organizations
- `GET /api/organizations/{id}` - Get organization details by ID

### Authenticated Endpoints (JWT Token Required)

- `GET /api/organizations/my-organizations` - Get organizations created by the authenticated user
- `POST /api/organizations` - Create a new organization
- `PUT /api/organizations/{id}` - Update an organization (only creator can update)
- `DELETE /api/organizations/{id}` - Delete an organization (only creator can delete)

## Configuration

The service requires the following configuration in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DonationBox_Organizations;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Services": {
    "AuthService": "http://localhost:5001"
  },
  "Auth": {
    "Authority": "https://localhost:5001"
  }
}
```

## Database Schema

The service uses a SQL Server database with the following main table:

### Organizations Table
- `Id` (Guid, Primary Key)
- `Name` (string, Required, Max 200 chars)
- `Description` (string, Optional, Max 1000 chars)
- `Address` (string, Optional, Max 500 chars)
- `Phone` (string, Optional, Max 20 chars)
- `Email` (string, Optional, Max 255 chars)
- `Website` (string, Optional, Max 500 chars)
- `CreatedBy` (Guid, Required) - References the user who created the organization
- `CreatedAt` (DateTime, Required)
- `UpdatedAt` (DateTime, Required)
- `IsActive` (bool, Required) - Soft delete flag

## Architecture

### Models
- `Organization` - Entity Framework entity for organizations

### DTOs
- `CreateOrganizationRequest` - Request DTO for creating organizations
- `UpdateOrganizationRequest` - Request DTO for updating organizations
- `OrganizationResponse` - Response DTO for organization data
- `OrganizationSummary` - Summary DTO for organization listings

### Services
- `IOrganizationService` - Interface for organization business logic
- `OrganizationService` - Implementation of organization operations
- `IAuthValidationService` - Interface for token validation
- `GrpcAuthValidationService` - HTTP client for communicating with AuthService

### Data
- `OrganizationDbContext` - Entity Framework database context
- `DbInitializer` - Database initialization and seeding

### Controllers
- `OrganizationsController` - REST API endpoints with proper authorization

## Authentication Flow

1. Client sends JWT token in `Authorization: Bearer <token>` header
2. `AuthValidationAttribute` extracts token and validates it using `GrpcAuthValidationService`
3. `GrpcAuthValidationService` calls AuthService's `/api/auth/validate` HTTP endpoint
4. AuthService returns user information if token is valid
5. User information is stored in `HttpContext.Items` for controller access

## Authorization Rules

- **Create/Update/Delete**: Only the user who created the organization can perform these operations
- **Read**: Anyone can read organization information
- **List User's Organizations**: Users can only see organizations they created

## Development

### Prerequisites
- .NET 9.0 SDK
- SQL Server
- AuthService running on the configured URL

### Running the Service
1. Ensure SQL Server is running and accessible
2. Update connection string in `appsettings.json`
3. Ensure AuthService is running
4. Run `dotnet run` or use your IDE

### Testing
Use the provided `OrganizationService.http` file in VS Code with the REST Client extension for testing endpoints.

## Sample Usage

### Create Organization
```bash
POST /api/organizations
Authorization: Bearer <jwt_token>
Content-Type: application/json

{
  "name": "New Charity Foundation",
  "description": "Helping communities in need",
  "address": "123 Charity Lane, City, ST 12345",
  "phone": "(555) 123-4567",
  "email": "contact@newcharity.org",
  "website": "https://www.newcharity.org"
}
```

### Get All Organizations
```bash
GET /api/organizations
```

### Get User's Organizations
```bash
GET /api/organizations/my-organizations
Authorization: Bearer <jwt_token>
```

## Notes

- The service uses soft deletes (IsActive flag) for organizations
- All timestamps are stored in UTC
- Database is automatically migrated and seeded on startup in development mode
- The service communicates with AuthService via HTTP for authentication
- Uses custom `AuthValidationAttribute` instead of built-in ASP.NET Core JWT middleware

