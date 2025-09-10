# Microservice Separation: CampaignService from DonationService

## Overview

This document describes the architectural changes made to separate campaign management concerns from donation processing in the DonationBox system.

## Problem Statement

Originally, the DonationService handled both:
- Donation processing (creating, validating, and tracking donations)
- Campaign management (CRUD operations, statistics, status management)

This created tight coupling between two distinct business domains and violated the Single Responsibility Principle.

## Solution

### New Architecture

```
┌─────────────────┐    Events     ┌─────────────────┐
│  DonationService│──────────────►│  CampaignService│
│                 │               │                 │
│ • Process donations │               │ • Manage campaigns │
│ • Handle payments  │               │ • Track statistics │
│ • Validate donors  │               │ • Update amounts   │
│ • Publish events   │               │ • Consume events   │
└─────────────────┘               └─────────────────┘
         │                                 │
         └──────────────► [Message Broker] ◄────────────┘
```

### CampaignService Responsibilities

**Core Functionality:**
- Campaign CRUD operations
- Campaign status management
- Campaign statistics and reporting
- Progress tracking and analytics

**Event Processing:**
- Consumes `DonationPaymentCompletedEvent` from DonationService
- Updates campaign current amounts asynchronously
- Maintains eventual consistency

**API Endpoints:**
- `GET /api/campaigns` - List campaigns
- `POST /api/campaigns` - Create campaign
- `PUT /api/campaigns/{id}` - Update campaign
- `DELETE /api/campaigns/{id}` - Delete campaign
- `GET /api/campaigns/{id}/stats` - Get campaign statistics

### DonationService Changes

**Removed Functionality:**
- Campaign model and related entities
- Campaign CRUD operations
- Campaign statistics calculation
- Direct campaign amount updates

**Added Functionality:**
- Event publishing for payment completion
- `DonationPaymentCompletedEvent` publication
- Removal of campaign navigation properties

**Updated API:**
- Donation responses no longer include full campaign details
- Campaign information fetched separately when needed

## Event-Driven Communication

### Event Flow

1. **Donation Creation**: User creates donation via DonationService API
2. **Payment Processing**: Payment is processed and status updated
3. **Event Publication**: `DonationPaymentCompletedEvent` published when payment completes
4. **Event Consumption**: CampaignService consumes event and updates campaign amount
5. **Consistency**: Campaign statistics reflect donation amounts through eventual consistency

### Event Schema

```csharp
public class DonationPaymentCompletedEvent
{
    public int CampaignId { get; set; }
    public int DonationId { get; set; }
    public decimal Amount { get; set; }
    public string TransactionId { get; set; }
    public DateTime PaymentCompletedAt { get; set; }
    public string DonorEmail { get; set; }
}
```

## Database Changes

### CampaignService Database
- **New Database**: `CampaignServiceDb`
- **Entities**: `DonationCampaign`
- **Schema**: Isolated campaign data with proper indexes

### DonationService Database
- **Updated Schema**: Removed campaign entities
- **Entities**: `Donation` (simplified, no navigation to campaigns)
- **Data Integrity**: Foreign key constraints removed

## Benefits

### Maintainability
- **Single Responsibility**: Each service has one primary concern
- **Independent Deployment**: Services can be deployed separately
- **Technology Diversity**: Different tech stacks possible per service

### Scalability
- **Horizontal Scaling**: Services can scale independently
- **Resource Optimization**: Scale campaign-heavy vs donation-heavy workloads separately
- **Database Optimization**: Separate databases for different access patterns

### Reliability
- **Fault Isolation**: Failure in one service doesn't affect others
- **Eventual Consistency**: System remains functional during temporary outages
- **Circuit Breakers**: Prevent cascade failures

### Development Velocity
- **Team Autonomy**: Different teams can work on different services
- **Technology Choices**: Services can use different frameworks/tools
- **Independent Testing**: Services can be tested in isolation

## Implementation Details

### Files Created/Modified

#### CampaignService (New)
```
src/Services/CampaignService/
├── Controllers/CampaignsController.cs
├── Models/DonationCampaign.cs
├── DTOs/
│   ├── CampaignResponse.cs
│   ├── CreateCampaignRequest.cs
│   └── UpdateCampaignRequest.cs
├── Services/
│   ├── ICampaignService.cs
│   ├── CampaignService.cs
│   ├── IEventConsumer.cs
│   ├── EventConsumer.cs
│   └── GrpcAuthValidationService.cs
├── Data/
│   ├── CampaignDbContext.cs
│   └── DbInitializer.cs
├── Events/DonationPaymentCompletedEvent.cs
├── Program.cs
└── appsettings.json
```

#### DonationService (Modified)
```
src/Services/DonationService/
├── Models/Donation.cs (removed campaign navigation)
├── Controllers/DonationsController.cs (updated)
├── Services/DonationServiceImpl.cs (updated)
├── Data/DonationDbContext.cs (updated)
├── Events/DonationPaymentCompletedEvent.cs (new)
└── Program.cs (updated)
```

### Configuration Changes

#### CampaignService
- Separate database connection string
- AuthService gRPC endpoint configuration
- Redis caching configuration (optional)

#### DonationService
- Removed campaign service dependency
- Event publisher configuration maintained

## Migration Strategy

### Database Migration
1. **Backup existing data** from DonationService database
2. **Extract campaign data** into separate database
3. **Update DonationService** to remove campaign foreign keys
4. **Deploy CampaignService** with extracted data
5. **Test data consistency** between services

### Service Deployment
1. **Deploy CampaignService** first
2. **Update DonationService** with event publishing
3. **Configure message broker** for event communication
4. **Test event flow** between services
5. **Monitor eventual consistency**

## Future Considerations

### Message Broker Integration
Currently using in-memory event publishing. Consider integrating:
- **RabbitMQ** for reliable messaging
- **Azure Service Bus** for cloud-native solutions
- **Redis Pub/Sub** for simple scenarios

### Advanced Patterns
- **Saga Pattern** for complex transactions
- **CQRS** for read/write optimization
- **Event Sourcing** for audit trails

### Monitoring and Observability
- **Distributed Tracing** across services
- **Event Monitoring** for message flows
- **Consistency Checks** for data integrity

## Testing Strategy

### Unit Tests
- Service layer testing in isolation
- Event publishing/consumption testing
- Data access layer testing

### Integration Tests
- End-to-end event flow testing
- API contract testing
- Database integration testing

### Contract Testing
- API contract validation between services
- Event schema validation
- Consumer-driven contract testing

## Conclusion

The separation of CampaignService from DonationService establishes a solid foundation for scalable microservice architecture. The event-driven communication ensures loose coupling while maintaining data consistency through eventual consistency patterns.

This architecture enables independent scaling, deployment, and development of each service while maintaining the system's overall functionality and reliability.
