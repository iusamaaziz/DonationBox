# DonationBox API Gateway

A comprehensive API Gateway implementation using YARP (Yet Another Reverse Proxy) for the DonationBox microservices architecture.

## Overview

The API Gateway serves as the single entry point for all client requests to the DonationBox microservices. It provides:

- **Reverse Proxy**: Routes requests to appropriate microservices
- **Load Balancing**: Distributes traffic across service instances
- **Authentication & Authorization**: JWT token validation and forwarding
- **CORS Support**: Cross-origin resource sharing configuration
- **Health Checks**: Monitoring service health and availability
- **Request Logging**: Comprehensive logging of incoming/outgoing requests
- **Swagger Documentation**: API documentation for all services

## Architecture

```
┌─────────────────┐    ┌──────────────────┐
│   Client Apps   │────│   API Gateway    │
│                 │    │  (Port: 5000)    │
└─────────────────┘    └──────────────────┘
                                │
                ┌───────────────┼───────────────┐
                │               │               │
        ┌───────▼──────┐ ┌──────▼──────┐ ┌─────▼─────┐
        │ Auth Service │ │Donation Svc │ │Payment Svc│
        │ (Port: 5002) │ │ (Port: 5131)│ │(Port: 5296)│
        └──────────────┘ └─────────────┘ └───────────┘
                │
        ┌───────▼──────┐
        │ Donor Service│
        │ (Port: 5069) │
        └──────────────┘
```

## Service Routes

The API Gateway routes requests based on the following patterns:

### Authentication Service
- `GET/POST /api/auth/*` → AuthService (Port 5002)
- `GET/POST /api/users/*` → AuthService (Port 5002)

### Donation Service
- `GET/POST/PUT/DELETE /api/campaigns/*` → DonationService (Port 5131)
- `GET/POST /api/donations/*` → DonationService (Port 5131)

### Donor Service
- `GET/POST/PUT/DELETE /api/donors/*` → DonorService (Port 5069)

### Payment Service
- `GET/POST /api/payment/*` → PaymentService (Port 5296)

## Features

### 1. Reverse Proxy with YARP
- Configurable routing rules
- Path transformations
- Load balancing support
- Health check integration

### 2. Authentication & Authorization
- JWT token validation
- Token forwarding to downstream services
- Configurable issuer, audience, and secret key

### 3. CORS Configuration
- Configurable allowed origins, methods, and headers
- Credentials support for authenticated requests

### 4. Health Monitoring
- Gateway health endpoint: `GET /health`
- Service health monitoring with automatic failover
- Detailed health status reporting

### 5. Request Logging
- Incoming request logging
- Outgoing response logging
- Configurable log levels

### 6. Swagger Integration
- Gateway API documentation at `/swagger`
- Service information endpoints

## Configuration

### appsettings.json

```json
{
  "ReverseProxy": {
    "Routes": {
      "auth-route": {
        "ClusterId": "auth-cluster",
        "Match": {
          "Path": "/api/auth/{**remainder}"
        }
      }
    },
    "Clusters": {
      "auth-cluster": {
        "Destinations": {
          "auth-service": {
            "Address": "http://localhost:5002"
          }
        },
        "HealthCheck": {
          "Active": {
            "Enabled": true,
            "Interval": "00:00:30",
            "Timeout": "00:00:10",
            "Path": "/health"
          }
        }
      }
    }
  },
  "JwtSettings": {
    "SecretKey": "your-jwt-secret-key",
    "Issuer": "ApiGateway",
    "Audience": "DonationBoxServices"
  },
  "CorsSettings": {
    "AllowedOrigins": ["http://localhost:3000", "https://localhost:3000"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": true
  }
}
```

## Running the API Gateway

### Prerequisites
- .NET 8.0 SDK
- All microservices running on their designated ports

### Steps
1. Navigate to the API Gateway directory:
   ```bash
   cd src/ApiGateway/ApiGateway
   ```

2. Restore dependencies:
   ```bash
   dotnet restore
   ```

3. Run the gateway:
   ```bash
   dotnet run
   ```

The API Gateway will start on:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:7000`

## API Endpoints

### Gateway-Specific Endpoints
- `GET /gateway/info` - Gateway information and service list
- `GET /gateway/status` - Gateway status and uptime
- `GET /health` - Health check endpoint
- `GET /swagger` - API documentation

### Service Routing Examples
- `POST /api/auth/login` → `POST http://localhost:5002/api/auth/login`
- `GET /api/campaigns` → `GET http://localhost:5131/api/campaigns`
- `POST /api/payment/process` → `POST http://localhost:5296/api/payment/process`

## Development

### Adding New Services
1. Add service configuration to `appsettings.json`
2. Define routes in the `Routes` section
3. Configure cluster destinations
4. Update the `Clusters` section with health checks

### Testing
Use the provided `.http` file or tools like Postman/cURL:

```bash
# Test gateway health
curl http://localhost:5000/health

# Test authentication
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}'

# Test campaigns
curl http://localhost:5000/api/campaigns
```

## Monitoring & Logging

### Health Checks
The gateway includes comprehensive health monitoring:
- Service availability
- Response times
- Error rates
- Circuit breaker status

### Logging
Logs include:
- Incoming request details (method, path, headers)
- Outgoing response status codes
- Service health status changes
- Authentication/authorization events

## Security

### Authentication
- JWT token validation at gateway level
- Token forwarding to downstream services
- Configurable token expiration and refresh

### CORS
- Configurable cross-origin policies
- Support for credentials and preflight requests

### HTTPS
- SSL termination support
- Configurable certificate management

## Troubleshooting

### Common Issues

1. **Service Unavailable**
   - Check if downstream services are running
   - Verify service URLs in configuration
   - Check health check endpoints

2. **Authentication Errors**
   - Verify JWT configuration
   - Check token validity and expiration
   - Ensure correct issuer/audience settings

3. **CORS Issues**
   - Verify CORS configuration
   - Check allowed origins and methods
   - Ensure credentials settings match client requirements

### Logs
Check the console output for detailed error information:
```bash
dotnet run --console
```

## Contributing

1. Follow the existing configuration patterns
2. Add appropriate health checks for new services
3. Update documentation for new routes
4. Test with all downstream services

## License

This project is part of the DonationBox microservices suite.
