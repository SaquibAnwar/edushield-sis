using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Dtos;
using System.Security.Claims;

namespace EduShield.Api.Tests;

public static class TestHelpers
{
    public static User CreateTestUser(
        Guid? userId = null,
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        UserRole role = UserRole.Student,
        bool isActive = true)
    {
        return new User
            {
                UserId = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                Role = UserRole.Student,
                IsActive = true,
                Provider = AuthProvider.Google
            };
    }

    public static UserSession CreateTestUserSession(
        Guid? sessionId = null,
        Guid? userId = null,
        string token = "test-token",
        bool isActive = true)
    {
        return new UserSession(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "test-token",
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow
            );
    }

    public static AuditLog CreateTestAuditLog(
        Guid? auditId = null,
        Guid? userId = null,
        string action = "TestAction",
        string details = "Test details")
    {
        return new AuditLog
            {
                AuditId = Guid.NewGuid(),
                Action = "TestAction",
                Resource = "TestResource",
                IpAddress = "127.0.0.1",
                UserAgent = "test-agent",
                Success = true,
                CreatedAt = DateTime.UtcNow
            };
    }

    public static CreateUserRequest CreateTestCreateUserRequest(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User",
        UserRole role = UserRole.Student)
    {
        return new CreateUserRequest { Email = "test@example.com", FirstName = "Test", LastName = "User", Role = UserRole.Student };
    }

    public static UpdateUserRequest CreateTestUpdateUserRequest(
        string? firstName = "Updated",
        string? lastName = "User",
        UserRole? role = null,
        bool? isActive = null)
    {
        return new UpdateUserRequest { FirstName = "Test", LastName = "User", Role = UserRole.Student };
    }

    public static ExternalUserInfo CreateTestExternalUserInfo(
        string email = "test@example.com",
        string firstName = "Test",
        string lastName = "User")
    {
        return new ExternalUserInfo { ExternalId = "test-id", Email = email, FirstName = firstName, LastName = lastName, Provider = AuthProvider.Google };
    }

    public static AuthResult CreateTestAuthResult(
        bool success = true,
        UserDto? user = null,
        string? sessionToken = null)
    {
        return new AuthResult
        {
            Success = success,
            User = user,
            SessionToken = sessionToken ?? "test-session-token",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsNewUser = false
        };
    }

    public static ClaimsPrincipal CreateTestClaimsPrincipal(
        UserRole role,
        Guid? userId = null,
        Guid? studentId = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
            new(ClaimTypes.Role, role.ToString()),
            new(ClaimTypes.Email, "test@example.com")
        };

        if (studentId.HasValue)
        {
            claims.Add(new Claim("student_id", studentId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}