# Fee Management System Design

## Overview

The Fee Management system provides comprehensive functionality for managing student fees, payments, and financial tracking within the EduShield SIS. The system follows the established architectural patterns of the application, implementing a clean architecture with separate layers for entities, DTOs, services, repositories, and controllers.

The system will handle multiple fee types, payment tracking, balance calculations, and provide both administrative and student-facing functionality through RESTful API endpoints.

## Architecture

The Fee Management system follows the existing EduShield architecture:

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Controllers   │────│    Services     │────│  Repositories   │
│                 │    │                 │    │                 │
│ FeeController   │    │ IFeeService     │    │ IFeeRepo        │
│                 │    │ FeeService      │    │ FeeRepo         │
└─────────────────┘    └─────────────────┘    └─────────────────┘
         │                       │                       │
         │                       │                       │
         ▼                       ▼                       ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│      DTOs       │    │   Validation    │    │    Entities     │
│                 │    │                 │    │                 │
│ CreateFeeReq    │    │ Validators      │    │ Fee             │
│ FeeDto          │    │                 │    │ Payment         │
│ PaymentReq      │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
```

## Components and Interfaces

### Core Entities

#### Fee Entity
```csharp
public class Fee : AuditableEntity
{
    public Guid FeeId { get; set; }
    public Guid StudentId { get; set; }
    public FeeType FeeType { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateTime DueDate { get; set; }
    public string Description { get; set; }
    public FeeStatus Status { get; set; }
    
    // Navigation properties
    public Student? Student { get; set; }
    public ICollection<Payment> Payments { get; set; }
    
    // Calculated properties
    public decimal OutstandingAmount => Amount - PaidAmount;
    public bool IsOverdue => DateTime.UtcNow > DueDate && OutstandingAmount > 0;
}
```

#### Payment Entity
```csharp
public class Payment : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public Guid FeeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; }
    public string? TransactionReference { get; set; }
    
    // Navigation properties
    public Fee? Fee { get; set; }
}
```

#### Enums
```csharp
public enum FeeType
{
    Tuition = 0,
    LabFee = 1,
    LibraryFee = 2,
    ActivityFee = 3,
    Other = 4
}

public enum FeeStatus
{
    Pending = 0,
    Paid = 1,
    Overdue = 2,
    PartiallyPaid = 3
}
```

### DTOs

#### Request DTOs
- `CreateFeeReq`: For creating new fees
- `UpdateFeeReq`: For updating existing fees
- `PaymentReq`: For recording payments

#### Response DTOs
- `FeeDto`: Complete fee information with calculated fields
- `PaymentDto`: Payment information
- `FeesSummaryDto`: Summary of student's financial status

### Repository Layer

#### IFeeRepo Interface
```csharp
public interface IFeeRepo
{
    Task<Fee?> GetByIdAsync(Guid feeId);
    Task<IEnumerable<Fee>> GetAllAsync();
    Task<IEnumerable<Fee>> GetByStudentIdAsync(Guid studentId);
    Task<IEnumerable<Fee>> GetByFeeTypeAsync(FeeType feeType);
    Task<IEnumerable<Fee>> GetOverdueFeesAsync();
    Task<Fee> CreateAsync(Fee fee);
    Task<Fee> UpdateAsync(Fee fee);
    Task<bool> DeleteAsync(Guid feeId);
    Task<Payment> AddPaymentAsync(Payment payment);
    Task<IEnumerable<Payment>> GetPaymentsByFeeIdAsync(Guid feeId);
}
```

### Service Layer

#### IFeeService Interface
```csharp
public interface IFeeService
{
    Task<FeeDto?> GetFeeByIdAsync(Guid feeId);
    Task<IEnumerable<FeeDto>> GetAllFeesAsync();
    Task<IEnumerable<FeeDto>> GetFeesByStudentIdAsync(Guid studentId);
    Task<IEnumerable<FeeDto>> GetFeesByTypeAsync(FeeType feeType);
    Task<IEnumerable<FeeDto>> GetOverdueFeesAsync();
    Task<FeeDto> CreateFeeAsync(CreateFeeReq request);
    Task<FeeDto> UpdateFeeAsync(Guid feeId, UpdateFeeReq request);
    Task<bool> DeleteFeeAsync(Guid feeId);
    Task<PaymentDto> RecordPaymentAsync(Guid feeId, PaymentReq request);
    Task<FeesSummaryDto> GetStudentFeesSummaryAsync(Guid studentId);
}
```

## Data Models

### Database Schema

#### Fees Table
- FeeId (Primary Key, GUID)
- StudentId (Foreign Key to Students, GUID)
- FeeType (Enum, INT)
- Amount (DECIMAL(10,2))
- PaidAmount (DECIMAL(10,2))
- DueDate (DATETIME)
- Description (NVARCHAR(500))
- Status (Enum, INT)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)

#### Payments Table
- PaymentId (Primary Key, GUID)
- FeeId (Foreign Key to Fees, GUID)
- Amount (DECIMAL(10,2))
- PaymentDate (DATETIME)
- PaymentMethod (NVARCHAR(50))
- TransactionReference (NVARCHAR(100), Nullable)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)

### Relationships
- Student (1) → Fees (Many)
- Fee (1) → Payments (Many)

## Error Handling

### Validation Rules
1. **Fee Amount**: Must be positive, maximum 2 decimal places
2. **Due Date**: Cannot be in the past for new fees
3. **Student ID**: Must reference existing student
4. **Payment Amount**: Cannot exceed outstanding fee amount
5. **Fee Type**: Must be valid enum value

### Error Responses
- 400 Bad Request: Validation errors, invalid data
- 404 Not Found: Fee or student not found
- 409 Conflict: Payment exceeds outstanding amount
- 500 Internal Server Error: System errors

### Exception Types
- `FeeNotFoundException`
- `StudentNotFoundException`
- `InvalidPaymentAmountException`
- `FeeValidationException`

## Testing Strategy

### Unit Tests
1. **Repository Tests**
   - CRUD operations for fees and payments
   - Query methods (by student, by type, overdue)
   - Data validation and constraints

2. **Service Tests**
   - Business logic validation
   - Payment processing logic
   - Fee status calculations
   - Error handling scenarios

3. **Controller Tests**
   - HTTP endpoint functionality
   - Request/response mapping
   - Authentication and authorization
   - Error response formatting

### Integration Tests
1. **Database Integration**
   - Entity Framework operations
   - Transaction handling
   - Relationship integrity

2. **API Integration**
   - End-to-end API workflows
   - Payment processing flows
   - Fee lifecycle management

### Test Data
- Sample students with various fee scenarios
- Different fee types and amounts
- Payment histories with partial and full payments
- Overdue fee scenarios

## API Endpoints

### Fee Management Endpoints
```
GET    /api/v1/fees                    # Get all fees
GET    /api/v1/fees/{id}               # Get fee by ID
GET    /api/v1/fees/student/{studentId} # Get fees by student
GET    /api/v1/fees/type/{feeType}     # Get fees by type
GET    /api/v1/fees/overdue            # Get overdue fees
POST   /api/v1/fees                    # Create new fee
PUT    /api/v1/fees/{id}               # Update fee
DELETE /api/v1/fees/{id}               # Delete fee
```

### Payment Endpoints
```
POST   /api/v1/fees/{id}/payments      # Record payment
GET    /api/v1/fees/{id}/payments      # Get payment history
GET    /api/v1/students/{id}/fees/summary # Get student fee summary
```

## Security Considerations

1. **Authorization**: Role-based access control for administrative functions
2. **Data Validation**: Comprehensive input validation and sanitization
3. **Audit Trail**: All fee and payment operations logged
4. **Financial Data**: Secure handling of monetary amounts and payment information