# Production Authentication & Authorization System Design

## Overview

The Production Authentication & Authorization system will implement a secure, scalable authentication solution using OpenID Connect (OIDC) with external identity providers. The system will support Google and Microsoft authentication, implement comprehensive role-based access control, and provide secure session management. The design follows industry best practices for security, maintainability, and integration with the existing EduShield SIS architecture.

## Architecture

The authentication system follows a layered architecture that integrates seamlessly with the existing application:

```
┌─────────────────────────────────────────────────────────────────┐
│                    External Identity Providers                  │
│              Google OAuth 2.0    Microsoft Azure AD            │
└─────────────────────┬───────────────────────────────────────────┘
                      │ OIDC Flow
┌─────────────────────▼───────────────────────────────────────────┐
│                 Authentication Middleware                       │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ OIDC Handler    │  │ JWT Validator   │  │ Session Manager │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                Authorization Layer                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │ Role Manager    │  │ Policy Engine   │  │ Claims Handler  │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────┬───────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────────┐
│                  Application Layer                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐ │
│  │   Controllers   │  │    Services     │  │  Repositories   │ │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Components and Interfaces

### Core Entities

#### User Entity
```csharp
public class User : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Email { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string ExternalId { get; set; } // From OIDC provider
    public string Provider { get; set; } // Google, Microsoft, etc.
    public UserRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? ProfilePictureUrl { get; set; }
    
    // Navigation properties
    public ICollection<UserSession> Sessions { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; }
}
```

#### UserSession Entity
```csharp
public class UserSession : AuditableEntity
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string SessionToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public bool IsActive { get; set; }
    
    // Navigation properties
    public User User { get; set; }
}
```

#### AuditLog Entity
```csharp
public class AuditLog : AuditableEntity
{
    public Guid AuditId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; }
    public string Resource { get; set; }
    public string IpAddress { get; set; }
    public string UserAgent { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AdditionalData { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
}
```

#### Enums
```csharp
public enum UserRole
{
    Student = 0,
    Parent = 1,
    Teacher = 2,
    SchoolAdmin = 3,
    SystemAdmin = 4
}

public enum AuthProvider
{
    Google = 0,
    Microsoft = 1,
    Custom = 2
}
```

### Authentication Services

#### IAuthService Interface
```csharp
public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string token, string provider);
    Task<User> GetOrCreateUserAsync(ExternalUserInfo userInfo);
    Task<bool> ValidateUserAsync(Guid userId);
    Task LogoutAsync(Guid userId, string sessionId);
    Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent);
    Task InvalidateSessionAsync(string sessionId);
    Task<bool> IsSessionValidAsync(string sessionId);
}
```

#### IUserService Interface
```csharp
public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid userId);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByExternalIdAsync(string externalId, string provider);
    Task<User> CreateUserAsync(CreateUserRequest request);
    Task<User> UpdateUserAsync(Guid userId, UpdateUserRequest request);
    Task<bool> DeactivateUserAsync(Guid userId);
    Task<bool> AssignRoleAsync(Guid userId, UserRole role);
    Task<IEnumerable<User>> GetUsersAsync(UserRole? role = null);
}
```

### Authorization Components

#### Custom Authorization Policies
```csharp
public static class AuthPolicies
{
    public const string SchoolAdminOnly = "SchoolAdminOnly";
    public const string TeacherOrAdmin = "TeacherOrAdmin";
    public const string StudentOwnerOrAdmin = "StudentOwnerOrAdmin";
    public const string ParentOrAdmin = "ParentOrAdmin";
}
```

#### Resource-Based Authorization Handlers
```csharp
public class StudentResourceAuthorizationHandler : 
    AuthorizationHandler<StudentAccessRequirement, Student>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StudentAccessRequirement requirement,
        Student resource)
    {
        // Implementation for student-specific access control
    }
}
```

### Configuration Models

#### OidcConfiguration
```csharp
public class OidcConfiguration
{
    public string Authority { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string[] Scopes { get; set; }
    public string ResponseType { get; set; }
    public bool RequireHttpsMetadata { get; set; }
    public TimeSpan TokenValidationTimeout { get; set; }
}
```

#### AuthenticationConfiguration
```csharp
public class AuthenticationConfiguration
{
    public Dictionary<string, OidcConfiguration> Providers { get; set; }
    public string DefaultProvider { get; set; }
    public TimeSpan SessionTimeout { get; set; }
    public bool EnableAuditLogging { get; set; }
    public bool AllowMultipleSessions { get; set; }
    public string CookieName { get; set; }
    public bool RequireSecureCookies { get; set; }
}
```

## Data Models

### Database Schema

#### Users Table
- UserId (Primary Key, GUID)
- Email (NVARCHAR(255), Unique)
- FirstName (NVARCHAR(100))
- LastName (NVARCHAR(100))
- ExternalId (NVARCHAR(255))
- Provider (NVARCHAR(50))
- Role (INT)
- IsActive (BIT)
- LastLoginAt (DATETIME, Nullable)
- ProfilePictureUrl (NVARCHAR(500), Nullable)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)

#### UserSessions Table
- SessionId (Primary Key, GUID)
- UserId (Foreign Key, GUID)
- SessionToken (NVARCHAR(500))
- ExpiresAt (DATETIME)
- IpAddress (NVARCHAR(45))
- UserAgent (NVARCHAR(500))
- IsActive (BIT)
- CreatedAt (DATETIME)
- UpdatedAt (DATETIME)

#### AuditLogs Table
- AuditId (Primary Key, GUID)
- UserId (Foreign Key, GUID, Nullable)
- Action (NVARCHAR(100))
- Resource (NVARCHAR(200))
- IpAddress (NVARCHAR(45))
- UserAgent (NVARCHAR(500))
- Success (BIT)
- ErrorMessage (NVARCHAR(1000), Nullable)
- AdditionalData (NTEXT, Nullable)
- CreatedAt (DATETIME)

### Relationships
- User (1) → UserSessions (Many)
- User (1) → AuditLogs (Many)

## Security Considerations

### Token Security
1. **JWT Validation**: Comprehensive validation of JWT tokens including signature, expiration, and issuer
2. **Secure Storage**: Use HTTP-only, secure cookies for token storage
3. **Token Rotation**: Implement refresh token rotation for enhanced security
4. **CSRF Protection**: Include anti-forgery tokens for state-changing operations

### Session Management
1. **Session Timeout**: Configurable session timeouts with automatic renewal
2. **Concurrent Sessions**: Optional support for multiple active sessions per user
3. **Session Invalidation**: Immediate session invalidation on logout or security events
4. **Session Monitoring**: Track and log all session activities

### Authorization Security
1. **Principle of Least Privilege**: Users get minimum required permissions
2. **Resource-Based Access**: Fine-grained access control based on resource ownership
3. **Role Hierarchy**: Clear role hierarchy with inheritance of permissions
4. **Dynamic Authorization**: Real-time authorization checks for sensitive operations

### Audit and Compliance
1. **Comprehensive Logging**: Log all authentication and authorization events
2. **Data Retention**: Configurable audit log retention policies
3. **Privacy Compliance**: GDPR-compliant user data handling
4. **Security Monitoring**: Real-time monitoring for suspicious activities

## Error Handling

### Authentication Errors
- Invalid or expired tokens
- OIDC provider communication failures
- User account deactivation
- Session timeout or invalidation

### Authorization Errors
- Insufficient permissions
- Resource access violations
- Role assignment conflicts
- Policy evaluation failures

### Exception Types
- `AuthenticationException`
- `AuthorizationException`
- `InvalidTokenException`
- `SessionExpiredException`
- `UserDeactivatedException`

## Testing Strategy

### Unit Tests
1. **Authentication Service Tests**
   - Token validation logic
   - User creation and management
   - Session management operations
   - Error handling scenarios

2. **Authorization Handler Tests**
   - Policy evaluation logic
   - Resource-based access control
   - Role-based permissions
   - Edge cases and security scenarios

### Integration Tests
1. **OIDC Integration Tests**
   - End-to-end authentication flows
   - Provider-specific token handling
   - Error scenarios and fallbacks

2. **Authorization Integration Tests**
   - Controller-level authorization
   - Cross-service authorization
   - Database integration for user management

### Security Tests
1. **Penetration Testing**
   - Token manipulation attempts
   - Session hijacking scenarios
   - Authorization bypass attempts

2. **Performance Tests**
   - Authentication performance under load
   - Session management scalability
   - Database query optimization

## API Endpoints

### Authentication Endpoints
```
GET    /api/v1/auth/login/{provider}        # Initiate OIDC login
GET    /api/v1/auth/callback/{provider}     # OIDC callback handler
POST   /api/v1/auth/logout                  # User logout
GET    /api/v1/auth/profile                 # Get current user profile
PUT    /api/v1/auth/profile                 # Update user profile
```

### User Management Endpoints (Admin Only)
```
GET    /api/v1/users                        # Get all users
GET    /api/v1/users/{id}                   # Get user by ID
POST   /api/v1/users                        # Create user (admin)
PUT    /api/v1/users/{id}                   # Update user
DELETE /api/v1/users/{id}                   # Deactivate user
PUT    /api/v1/users/{id}/role              # Assign role
GET    /api/v1/users/{id}/sessions          # Get user sessions
DELETE /api/v1/users/{id}/sessions/{sessionId} # Invalidate session
```

### Audit Endpoints (Admin Only)
```
GET    /api/v1/audit/logs                   # Get audit logs
GET    /api/v1/audit/users/{id}/logs        # Get user-specific logs
GET    /api/v1/audit/security-events        # Get security events
```

## Configuration Examples

### Google OIDC Configuration
```json
{
  "Authentication": {
    "Providers": {
      "Google": {
        "Authority": "https://accounts.google.com",
        "ClientId": "your-google-client-id",
        "ClientSecret": "your-google-client-secret",
        "Scopes": ["openid", "profile", "email"],
        "ResponseType": "code",
        "RequireHttpsMetadata": true,
        "TokenValidationTimeout": "00:05:00"
      }
    },
    "DefaultProvider": "Google",
    "SessionTimeout": "08:00:00",
    "EnableAuditLogging": true,
    "AllowMultipleSessions": false,
    "CookieName": "EduShield.Auth",
    "RequireSecureCookies": true
  }
}
```

### Microsoft Azure AD Configuration
```json
{
  "Authentication": {
    "Providers": {
      "Microsoft": {
        "Authority": "https://login.microsoftonline.com/common/v2.0",
        "ClientId": "your-azure-client-id",
        "ClientSecret": "your-azure-client-secret",
        "Scopes": ["openid", "profile", "email"],
        "ResponseType": "code",
        "RequireHttpsMetadata": true,
        "TokenValidationTimeout": "00:05:00"
      }
    }
  }
}
```

## Deployment Considerations

### Environment Configuration
- Development: Relaxed security for testing
- Staging: Production-like security with test data
- Production: Full security enforcement

### Secrets Management
- Use Azure Key Vault or AWS Secrets Manager
- Environment-specific secret rotation
- Secure configuration injection

### Monitoring and Alerting
- Authentication failure rate monitoring
- Suspicious activity detection
- Performance metrics tracking
- Security event alerting