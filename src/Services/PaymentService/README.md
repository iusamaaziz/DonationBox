# PaymentService

An Azure Functions-based microservice for processing donation payments with Durable Functions Saga orchestration, distributed locking, and reliable event delivery patterns.

## Features

- **🔄 Saga Orchestration**: Durable Functions-based Saga pattern for reliable payment processing with automatic compensation
- **🔒 Distributed Locking**: Redis-based distributed locks to prevent duplicate payments
- **📤 Outbox Pattern**: Reliable event delivery with automatic retry and exponential backoff
- **📊 Payment Ledger**: Complete audit trail of all payment transactions and operations
- **🎯 Multiple Payment Gateways**: Simulated support for various payment methods (Credit Card, PayPal, Bank Transfer, etc.)
- **🔧 Isolated Worker Runtime**: Modern Azure Functions v4 with .NET 8 isolated worker process
- **🏥 Health Monitoring**: Built-in health checks and monitoring capabilities

## Architecture

### Saga Orchestration Flow

```
Payment Request → Acquire Lock → Create Transaction → Process Payment → Update Status → Create Ledger → Confirm Donation → Publish Event → Release Lock
                      ↓ (on failure)
                 Compensate: Refund → Update Status → Publish Failed Event → Release Lock
```

### Key Components

- **PaymentSagaOrchestrator**: Coordinates the entire payment process
- **PaymentSagaActivities**: Individual steps in the payment saga
- **DistributedLockService**: Prevents duplicate payment processing
- **OutboxService**: Ensures reliable event delivery
- **PaymentGatewayService**: Handles payment gateway integration
- **PaymentDbContext**: Manages payment ledger and transaction data

## 📋 API Documentation (Swagger/OpenAPI)

The PaymentService provides comprehensive Swagger/OpenAPI documentation for all endpoints with interactive testing capabilities.

### 🌐 Swagger UI Access

**Local Development:**
- **Swagger UI**: http://localhost:7071/api/swagger/ui
- **OpenAPI v3 Spec**: http://localhost:7071/api/openapi/v3.json
- **OpenAPI v2 Spec**: http://localhost:7071/api/openapi/v2.json
- **Legacy Swagger JSON**: http://localhost:7071/api/swagger.json

**Production:**
- **Swagger UI**: https://your-paymentservice.azurewebsites.net/api/swagger/ui
- **OpenAPI v3 Spec**: https://your-paymentservice.azurewebsites.net/api/openapi/v3.json
- **OpenAPI v2 Spec**: https://your-paymentservice.azurewebsites.net/api/openapi/v2.json
- **Legacy Swagger JSON**: https://your-paymentservice.azurewebsites.net/api/swagger.json

### 🔐 Authentication

The API uses **API Key authentication** for most endpoints:

```http
x-api-key: your-api-key-here
```

**Public Endpoints** (no authentication required):
- `GET /api/info` - Service information
- `GET /api/swagger/ui` - Built-in Swagger UI
- `GET /api/openapi/v3.json` - OpenAPI v3 specification
- `GET /api/openapi/v2.json` - OpenAPI v2 specification
- `GET /api/swagger.json` - Legacy Swagger JSON specification

### 📚 API Endpoint Categories

#### 🏦 Payments
- **Process Payment**: `POST /api/payments/process`
  - Initiates Saga orchestration for payment processing
  - Returns orchestration ID for status tracking
  - **Request**: `ProcessPaymentRequest`
  - **Response**: Orchestration details with status URL

- **Get Payment Status**: `GET /api/payments/status/{instanceId}`
  - Retrieves orchestration status and progress
  - **Parameters**: `instanceId` (orchestration instance ID)
  - **Response**: Orchestration runtime status and output

- **Get Payment Details**: `GET /api/payments/{transactionId}`
  - Retrieves complete payment information with ledger entries
  - **Parameters**: `transactionId` (payment transaction ID)
  - **Response**: `PaymentStatusResponse` with ledger details

- **Initiate Refund**: `POST /api/payments/{transactionId}/refund`
  - Processes full or partial payment refunds
  - **Parameters**: `transactionId` (original payment transaction ID)
  - **Request**: `RefundRequest` (amount and reason)
  - **Response**: `RefundResponse` with refund tracking details

- **Get Payments by Donation**: `GET /api/payments/donation/{donationId}`
  - Lists all payments associated with a donation
  - **Parameters**: `donationId` (donation identifier)
  - **Response**: Array of `PaymentResponse` objects

#### 🔍 Health & Monitoring
- **Service Information**: `GET /api/info`
  - Returns service metadata, version, and capabilities
  - **Response**: Service information object with features list

### 📝 Request/Response Examples

#### Process Payment Request
```json
{
  "donationId": 123,
  "campaignId": 456,
  "amount": 100.00,
  "currency": "USD",
  "donorName": "John Doe",
  "donorEmail": "john.doe@example.com",
  "paymentMethod": "CreditCard",
  "paymentDetails": {
    "cardNumber": "****-****-****-1234",
    "expiryMonth": 12,
    "expiryYear": 2025,
    "cvv": "***"
  }
}
```

#### Payment Response
```json
{
  "orchestrationId": "abc123-def456-ghi789",
  "donationId": 123,
  "amount": 100.00,
  "status": "Processing",
  "statusCheckUrl": "/api/payments/status/abc123-def456-ghi789"
}
```

#### Payment Status Response
```json
{
  "transactionId": "TXN-PAY-20240115-ABC12345",
  "status": "Completed",
  "amount": 100.00,
  "currency": "USD",
  "createdAt": "2024-01-15T10:30:00Z",
  "completedAt": "2024-01-15T10:31:30Z",
  "ledgerEntries": [
    {
      "amount": 100.00,
      "entryType": "Credit",
      "operation": "PaymentReceived",
      "description": "Payment received for donation 123",
      "createdAt": "2024-01-15T10:31:30Z"
    }
  ]
}
```

### 🔧 Testing with Swagger UI

1. **Navigate to Swagger UI**: Open the Swagger UI URL in your browser
2. **Set API Key**: Click "Authorize" and enter your API key
3. **Explore Endpoints**: Browse available endpoints organized by tags
4. **Try It Out**: Use the interactive forms to test endpoints
5. **View Responses**: See real-time responses with status codes and data

### 📊 Response Status Codes

- **200 OK**: Request successful
- **201 Created**: Resource created successfully  
- **202 Accepted**: Request accepted for processing
- **400 Bad Request**: Invalid request data
- **401 Unauthorized**: Missing or invalid API key
- **404 Not Found**: Resource not found
- **409 Conflict**: Duplicate request detected
- **500 Internal Server Error**: Server processing error

## API Endpoints (Legacy Reference)

### Payment Operations
- `POST /api/payments/process` - Process a new payment (starts Saga orchestration)
- `GET /api/payments/status/{instanceId}` - Get orchestration status
- `GET /api/payments/{transactionId}` - Get payment details and ledger
- `POST /api/payments/{transactionId}/refund` - Refund a payment
- `GET /api/payments/donation/{donationId}` - Get all payments for a donation

### Admin Operations
- `POST /api/admin/process-outbox` - Manually trigger outbox event processing
- `GET /api/info` - Service information and health

## Data Models

### PaymentTransaction
Core payment entity with status tracking, gateway integration, and audit fields.

### PaymentLedgerEntry
Detailed ledger entries for all payment operations (payments, fees, refunds, etc.).

### OutboxEvent
Reliable event delivery with retry logic and failure handling.

## Configuration

### Environment Variables

- `UseRedis`: Enable/disable Redis distributed locking (default: false)
- `Outbox:MaxRetries`: Maximum retry attempts for failed events (default: 5)
- `Outbox:ProcessingIntervalSeconds`: Outbox processing interval (default: 30)

### Connection Strings

- `DefaultConnection`: SQL Server connection for payment ledger
- `Redis`: Redis connection for distributed locking
- `AzureWebJobsStorage`: Azure Storage for Durable Functions state

### Example Configuration (local.settings.json)

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=PaymentServiceDb_Dev;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true",
    "Redis": "localhost:6379"
  },
  "UseRedis": true,
  "Outbox": {
    "MaxRetries": 5,
    "ProcessingIntervalSeconds": 30
  }
}
```

## Running the Service

### Prerequisites

- .NET 8 SDK
- Azure Functions Core Tools v4
- SQL Server (LocalDB for development)
- Azure Storage Emulator or Azurite
- Redis (optional, for distributed locking)

### Development

1. **Install Azure Functions Core Tools**:
   ```bash
   npm install -g azure-functions-core-tools@4 --unsafe-perm true
   ```

2. **Start dependencies**:
   ```bash
   # Start Azure Storage Emulator
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log

   # Start Redis (optional)
   redis-server
   ```

3. **Run the service**:
   ```bash
   cd src/Services/PaymentService
   func start
   ```

4. **Test endpoints**:
   - Service runs on `http://localhost:7071`
   - API endpoints available at `http://localhost:7071/api/*`

### Database

The service uses Entity Framework Core with SQL Server. The database is automatically created and seeded with sample data on first run.

## Payment Processing Flow

### 1. Payment Request
```json
POST /api/payments/process
{
  "donationId": 1,
  "campaignId": 1,
  "amount": 100.00,
  "currency": "USD",
  "donorName": "John Doe",
  "donorEmail": "john@example.com",
  "paymentMethod": 0,
  "paymentDetails": {
    "cardNumber": "4242424242424242",
    "expiryDate": "12/25",
    "cvv": "123",
    "cardHolderName": "John Doe"
  }
}
```

### 2. Orchestration Response
```json
{
  "orchestrationId": "abc123...",
  "donationId": 1,
  "amount": 100.00,
  "status": "Processing",
  "statusCheckUrl": "/api/payments/status/abc123..."
}
```

### 3. Status Tracking
```json
GET /api/payments/status/abc123...
{
  "orchestrationId": "abc123...",
  "runtimeStatus": "Completed",
  "createdTime": "2024-01-01T12:00:00Z",
  "lastUpdatedTime": "2024-01-01T12:01:00Z",
  "output": "{ ... payment response ... }"
}
```

## Saga Patterns Implemented

### 1. **Distributed Lock Pattern**
- Prevents duplicate payment processing
- Redis-based with fallback to in-memory locking
- Automatic lock expiration and cleanup

### 2. **Saga Orchestration Pattern**
- Coordinates multi-step payment process
- Automatic compensation on failures
- Durable execution with state persistence

### 3. **Outbox Pattern**
- Reliable event publishing
- Automatic retry with exponential backoff
- Dead letter handling for failed events

### 4. **Circuit Breaker Pattern**
- Gateway failure handling
- Automatic fallback and recovery
- Health monitoring integration

## Event-Driven Architecture

### Published Events

- **PaymentProcessedEvent**: Payment gateway processing completed
- **PaymentCompletedEvent**: Full payment saga completed successfully  
- **PaymentFailedEvent**: Payment processing failed
- **PaymentRefundedEvent**: Payment refund processed

### Event Delivery

- Events stored in outbox table
- Background processor ensures delivery
- Retry logic with exponential backoff
- Dead letter queue for permanent failures

## Monitoring and Observability

### Logging
- Structured logging with correlation IDs
- Performance metrics and timing
- Error tracking and alerting
- Saga execution tracing

### Health Checks
- Database connectivity
- Redis availability (when enabled)
- Gateway service health
- Outbox processing status

### Metrics
- Payment success/failure rates
- Processing times and throughput
- Lock contention and wait times
- Event delivery success rates

## Security Considerations

### Payment Data
- PCI DSS compliance patterns
- Sensitive data encryption
- Secure gateway communication
- Audit trail requirements

### Access Control
- Function-level authorization
- API key authentication
- Role-based access control
- Request validation and sanitization

## Deployment

### Azure Deployment
```bash
# Build and package
dotnet publish -c Release

# Deploy to Azure Functions
func azure functionapp publish <function-app-name>
```

### Configuration
- Use Azure Key Vault for secrets
- Configure managed identity for secure access
- Set up Application Insights for monitoring
- Configure auto-scaling based on demand

## Testing

### Unit Tests
- Payment gateway simulation
- Saga orchestration logic
- Distributed lock behavior
- Outbox event processing

### Integration Tests
- End-to-end payment flows
- Failure scenario testing
- Performance and load testing
- Database migration testing

## Troubleshooting

### Common Issues

1. **Lock Acquisition Failures**
   - Check Redis connectivity
   - Verify lock expiration settings
   - Monitor lock contention metrics

2. **Saga Execution Failures**
   - Review orchestration logs
   - Check activity function errors
   - Verify compensation logic

3. **Event Delivery Issues**
   - Monitor outbox processing
   - Check retry configurations
   - Review dead letter events

### Debugging Tools

- Durable Functions Monitor
- Application Insights queries
- Database query analysis
- Redis monitoring commands

## Contributing

The PaymentService follows microservices best practices and domain-driven design principles. When extending functionality:

1. Maintain saga pattern consistency
2. Ensure proper error handling and compensation
3. Add comprehensive logging and monitoring
4. Include unit and integration tests
5. Update documentation and API contracts

## License

This project is part of the DonationBox microservices learning system.
