# AuthService

A .NET 8 microservice for user authentication with JWT tokens and Google OAuth support in the DonationBox system.

## Features

- **User Registration**: Create new user accounts with email and password
- **User Authentication**: Login with email and password
- **Google OAuth**: Sign in/up with Google accounts
- **JWT Tokens**: Secure access tokens with refresh token support
- **Token Validation**: Validate JWT tokens for other microservices via REST API and gRPC
- **gRPC Integration**: High-performance gRPC service for microservice communication
- **SQL Server Database**: Entity Framework Core with SQL Server for data persistence
- **Password Security**: BCrypt password hashing
- **Health Checks**: Built-in health checks for database connectivity

## API Endpoints

### Authentication
- `POST /api/auth/login` - Authenticate with email and password
- `POST /api/auth/register` - Register a new user account
- `POST /api/auth/google` - Authenticate with Google OAuth token
- `POST /api/auth/refresh` - Refresh access token using refresh token
- `POST /api/auth/validate` - Validate a JWT token (used by other microservices)
- `POST /api/auth/revoke` - Revoke a refresh token

### Users
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users/by-email/{email}` - Get user by email

### System
- `GET /health` - Health check endpoint
- `GET /info` - Service information

### gRPC Services
- `ValidateToken(ValidateTokenRequest)` - Validate JWT token (for microservice communication)
- `GetUser(GetUserRequest)` - Get user by ID
- `GetUserByEmail(GetUserByEmailRequest)` - Get user by email

## Configuration

### Environment Variables

Configure the following settings in `appsettings.json` or `appsettings.Development.json`:

### Connection Strings

- `DefaultConnection`: SQL Server connection string

### JWT Settings

- `SecretKey`: Secret key for JWT token signing (must be at least 256 bits)
- `Issuer`: JWT token issuer
- `Audience`: JWT token audience
- `AccessTokenExpirationMinutes`: Access token expiration time (default: 15 minutes)
- `RefreshTokenExpirationDays`: Refresh token expiration time (default: 30 days)

### Google OAuth Settings

- `ClientId`: Google OAuth client ID
- `ClientSecret`: Google OAuth client secret

### Example Configuration

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AuthServiceDb;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "JwtSettings": {
    "SecretKey": "your-super-secret-jwt-key-that-is-at-least-256-bits-long!",
    "Issuer": "AuthService",
    "Audience": "DonationBoxClients",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 30
  },
  "GoogleAuth": {
    "ClientId": "your-google-oauth-client-id",
    "ClientSecret": "your-google-oauth-client-secret"
  }
}
```

## Running the Service

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB for development)
- Google OAuth credentials (optional, for Google sign-in)

### Development

1. **Clone and navigate to the project**:
   ```bash
   cd src/Services/AuthService
   ```

2. **Restore packages**:
   ```bash
   dotnet restore
   ```

3. **Update configuration**:
   - Edit `appsettings.Development.json`
   - Set your Google OAuth credentials if using Google sign-in
   - Configure your SQL Server connection string

4. **Run the service**:
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**:
   - Navigate to `https://localhost:7002` or `http://localhost:5002`

### Database

The service uses Entity Framework Core with SQL Server. The database is automatically created and seeded with sample data on first run.

### Sample Users

The database initializer creates the following sample users:

- **Admin User**: admin@donationbox.com / Admin123!
- **John Doe**: john.doe@example.com / Password123!
- **Jane Smith**: jane.smith@example.com / Password123!
- **Google User**: google.user@gmail.com (Google OAuth only)

## Integration with Other Services

### DonationService Integration

The DonationService is configured to validate authentication tokens with this AuthService via gRPC:

1. **Authentication Required**: Sensitive operations (create/update/delete campaigns, create donations) require valid JWT tokens
2. **gRPC Communication**: The DonationService uses gRPC to call the `ValidateToken` service for high-performance token verification
3. **User Context**: Authenticated user information is available in controllers
4. **Fallback Support**: REST API endpoints are also available for HTTP-based integration if needed

### Usage Example

```bash
# 1. Register or login to get tokens
curl -X POST https://localhost:7002/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email": "john.doe@example.com", "password": "Password123!"}'

# Response includes accessToken and refreshToken

# 2. Use the access token in DonationService requests
curl -X POST https://localhost:5001/api/campaigns \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"title": "New Campaign", "description": "...", "goalAmount": 10000}'
```

## Google OAuth Setup

To enable Google OAuth:

1. **Create Google OAuth Credentials**:
   - Go to [Google Cloud Console](https://console.cloud.google.com/)
   - Create a new project or select existing
   - Enable Google+ API
   - Create OAuth 2.0 credentials
   - Add authorized redirect URIs

2. **Configure the Service**:
   - Set `GoogleAuth:ClientId` and `GoogleAuth:ClientSecret` in configuration
   - The service will automatically enable Google authentication

3. **Client Integration**:
   - Use Google Sign-In JavaScript library in your frontend
   - Send the Google ID token to `/api/auth/google` endpoint

## Security Features

- **Password Hashing**: BCrypt with salt for secure password storage
- **JWT Security**: Signed tokens with configurable expiration
- **Refresh Tokens**: Secure token refresh mechanism with revocation support
- **Google OAuth**: Secure integration with Google authentication
- **Token Validation**: Comprehensive token validation for microservice communication

## Architecture

- **Controllers**: REST API endpoints for authentication operations
- **Services**: Business logic layer with authentication and JWT services
- **Data**: Entity Framework DbContext and models
- **Models**: User and RefreshToken entities
- **DTOs**: Request/response models for API endpoints

## Technology Stack

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server
- JWT Bearer Authentication
- Google OAuth
- BCrypt.Net for password hashing
- Swagger/OpenAPI for documentation
