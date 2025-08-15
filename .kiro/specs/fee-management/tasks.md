c# Fee Management System Implementation Plan

- [x] 1. Create core entities and enums
  - Create FeeType and FeeStatus enums in the Enums directory
  - Implement Fee entity with all properties, navigation properties, and calculated fields
  - Implement Payment entity with relationships to Fee
  - Update Student entity to include Fee navigation property
  - _Requirements: 1.1, 1.2, 1.3, 2.1, 3.1_

- [x] 2. Create DTOs and request models
  - Implement CreateFeeReq DTO with validation attributes
  - Implement UpdateFeeReq DTO for fee modifications
  - Implement PaymentReq DTO for payment recording
  - Implement FeeDto response DTO with calculated properties
  - Implement PaymentDto response DTO
  - Implement FeesSummaryDto for student financial summaries
  - _Requirements: 1.1, 1.2, 2.1, 3.1, 4.4, 5.4_

- [x] 3. Implement validation rules
  - Create CreateFeeReqValidator with amount, due date, and student ID validation
  - Create UpdateFeeReqValidator with business rule validations
  - Create PaymentReqValidator with amount and payment method validation
  - Add custom validation for fee amount precision and positive values
  - Add validation to prevent payments exceeding outstanding amounts
  - _Requirements: 1.3, 1.4, 3.2, 6.1, 6.4_

- [x] 4. Create AutoMapper profiles
  - Implement FeeMappingProfile for Fee entity to FeeDto mapping
  - Add mapping for calculated properties (OutstandingAmount, IsOverdue)
  - Implement PaymentMappingProfile for Payment entity mappings
  - Add mapping configurations for request DTOs to entities
  - Include navigation property mappings for Student relationships
  - _Requirements: 2.1, 4.1, 5.1, 5.2_

- [x] 5. Implement repository interfaces and implementations
  - Create IFeeRepo interface with all CRUD and query methods
  - Implement FeeRepo with Entity Framework operations
  - Add methods for querying by student ID, fee type, and overdue status
  - Implement payment-related repository methods
  - Add proper error handling and null checks in repository methods
  - _Requirements: 1.5, 2.1, 3.1, 4.1, 4.2, 4.3, 4.4_

- [x] 6. Implement service interfaces and business logic
  - Create IFeeService interface with all business operations
  - Implement FeeService with comprehensive business logic
  - Add fee status calculation and update logic
  - Implement payment processing with balance calculations
  - Add validation for business rules (payment limits, fee modifications)
  - Implement student fee summary calculations
  - _Requirements: 1.1, 2.2, 2.4, 3.3, 3.4, 3.5, 4.5, 5.3, 5.4_

- [x] 7. Create API controller with endpoints
  - Implement FeeController with all REST endpoints
  - Add GET endpoints for retrieving fees by various criteria
  - Add POST endpoint for creating new fees
  - Add PUT endpoint for updating existing fees
  - Add DELETE endpoint for removing fees
  - Add POST endpoint for recording payments
  - Add proper HTTP status codes and error responses
  - _Requirements: 1.1, 2.1, 3.1, 4.1, 4.2, 4.3, 4.4, 5.1, 6.2, 6.3_

- [ ] 8. Update database context and configuration
  - Add Fee and Payment DbSets to ApplicationDbContext
  - Configure entity relationships and constraints
  - Add database migrations for new tables
  - Configure decimal precision for monetary amounts
  - Set up proper indexes for query performance
  - _Requirements: 1.1, 1.5, 2.1, 3.1_

- [ ] 9. Register services in dependency injection
  - Register IFeeRepo and FeeRepo in Program.cs
  - Register IFeeService and FeeService in Program.cs
  - Add AutoMapper profile registration
  - Configure FluentValidation for fee-related validators
  - _Requirements: All requirements - system integration_

- [ ] 10. Create comprehensive repository tests
  - Write unit tests for FeeRepo CRUD operations
  - Test query methods (by student, by type, overdue fees)
  - Test payment recording functionality
  - Test entity relationships and navigation properties
  - Test error scenarios and edge cases
  - _Requirements: 1.5, 2.1, 3.1, 4.1, 4.2, 4.3, 4.4, 6.2, 6.3_

- [ ] 11. Create comprehensive service tests
  - Write unit tests for FeeService business logic
  - Test fee creation with validation
  - Test payment processing and balance calculations
  - Test fee status updates and calculations
  - Test student fee summary generation
  - Test error handling and exception scenarios
  - Mock repository dependencies properly
  - _Requirements: 1.2, 1.3, 2.2, 2.4, 3.2, 3.3, 3.4, 3.5, 4.5, 5.3, 5.4, 6.1, 6.4_

- [ ] 12. Create comprehensive controller tests
  - Write unit tests for FeeController endpoints
  - Test all HTTP methods (GET, POST, PUT, DELETE)
  - Test request/response mapping and validation
  - Test error response formatting and status codes
  - Test authentication and authorization scenarios
  - Mock service dependencies and test controller logic
  - _Requirements: 2.1, 4.1, 4.2, 4.3, 4.4, 5.1, 5.2, 6.1, 6.2, 6.3_

- [ ] 13. Create integration tests
  - Write end-to-end tests for complete fee management workflows
  - Test fee creation, payment recording, and status updates
  - Test database operations and transaction handling
  - Test API endpoints with real HTTP requests
  - Test error scenarios and data validation
  - _Requirements: All requirements - end-to-end validation_

- [ ] 14. Add comprehensive error handling
  - Implement custom exception classes (FeeNotFoundException, etc.)
  - Add global exception handling for fee-related errors
  - Ensure proper HTTP status codes for all error scenarios
  - Add detailed error messages and validation feedback
  - Test all error handling paths
  - _Requirements: 6.1, 6.2, 6.3, 6.4, 6.5_