# Payment Gateway Challenge

##  Architecture Overview

This project follows **Clean Architecture** and **Domain-Driven Design (DDD)** principles, ensuring separation of concerns, maintainability, and testability.

### Project Structure
PaymentGateway/

```
├── src/
├── PaymentGateway.Application/ # Web API & Application Services 
│   ├── Controllers/ # API Controllers 
│   └── Services/ # Application Services 
├── PaymentGateway.Domain/ # Domain Layer │
│   ├── Entities/ # Domain Entities │
│   ├── Enums/ # Domain Enumerations │
│   └── Interfaces/ # Domain Interfaces
├── PaymentGateway.Infrastructure/ # Infrastructure Layer │
│   ├── Repository/ # Data Access │
│   └── HttpClients/ # External Service Clients
├── test/ # Test Projects 
    └── PaymentGateway.Api.Tests/ # Unit & Integration Tests
```

## Features

- **Payment Status Management**: Support for Authorized, Declined, and Rejected payment states
- **Multi-Currency Support**: Handle payments in USD, BRL, and GBP
- **External Bank Integration**: Integration with acquiring bank services
- **Data Persistence**: In-memory repository with extensible design
- **Comprehensive Validation**: Input validation at domain level
- **RESTful API**: Clean and intuitive REST endpoints

## API Endpoints

### Payment Processing

#### Process Payment
```http
POST /api/payments
{
  "cardNumber": "1234567890123456",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "USD",
  "amount": 1000,
  "cvv": "123"
}

Response (201 Created):

{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Authorized",
  "cardNumberLastFour": "3456",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "USD",
  "amount": 1000,
  "authorizationCode": "AUTH123456"
}
```

#### Retrieve Payment

```
GET /api/payments/{id}

Response (200 OK):

{
  "id": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Authorized",
  "cardNumberLastFour": "3456",
  "expiryMonth": 12,
  "expiryYear": 2025,
  "currency": "USD",
  "amount": 1000,
  "authorizationCode": "AUTH123456"
}
```

## Architecture Patterns
### Clean Architecture
The project is organized into distinct layers:
- **Domain Layer**: Contains business entities and domain interfaces
- **Application Layer**: Hosts application services, API controllers, and use cases
- **Infrastructure Layer**: Implements data access, external service integrations

### Domain-Driven Design (DDD)
- **Entities**: `Payment` entity encapsulates business logic and invariants
- **Value Objects**: Enums for `Currency` and `PaymentStatus`
- **Domain Services**: Interfaces defining business operations
- **Repository Pattern**: Abstraction for data persistence

## Domain Model
### Payment Entity
The core `Payment` entity ensures data integrity through:
- **Immutable Properties**: Most properties are read-only after creation
- **Input Validation**: Comprehensive validation in constructor and private methods
- **Business Rules**: Encapsulated payment status management logic

## Testing Strategy
### Unit Tests
- **Domain Entity Tests**: Comprehensive validation testing for `Payment` entity
- **Service Tests**: Business logic validation
- **Controller Tests**: API endpoint testing

### Test Coverage
- Input validation scenarios
- Business rule enforcement
- Edge cases and error conditions
- Success path verification