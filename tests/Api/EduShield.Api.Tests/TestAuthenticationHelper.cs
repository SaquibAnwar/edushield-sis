using EduShield.Core.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EduShield.Api.Tests;

public class TestAuthenticationHelper
{
    public static ClaimsPrincipal CreateTestUser(UserRole role, Guid? userId = null, Guid? studentId = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString()),
            new Claim(ClaimTypes.Role, role.ToString()),
            new Claim(ClaimTypes.Email, $"test-{role.ToString().ToLower()}@example.com"),
            new Claim(ClaimTypes.Name, $"Test {role}")
        };

        if (studentId.HasValue)
        {
            claims.Add(new Claim("StudentId", studentId.Value.ToString()));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    public static ClaimsPrincipal CreateSystemAdmin()
    {
        return CreateTestUser(UserRole.SystemAdmin);
    }

    public static ClaimsPrincipal CreateSchoolAdmin()
    {
        return CreateTestUser(UserRole.SchoolAdmin);
    }

    public static ClaimsPrincipal CreateTeacher()
    {
        return CreateTestUser(UserRole.Teacher);
    }

    public static ClaimsPrincipal CreateParent(Guid? userId = null)
    {
        return CreateTestUser(UserRole.Parent, userId);
    }

    public static ClaimsPrincipal CreateStudent(Guid? userId = null, Guid? studentId = null)
    {
        return CreateTestUser(UserRole.Student, userId, studentId);
    }
}

public class TestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
    public UserRole DefaultRole { get; set; } = UserRole.Student;
    public Guid? UserId { get; set; }
    public Guid? StudentId { get; set; }
}

public class TestAuthenticationHandler : AuthenticationHandler<TestAuthenticationSchemeOptions>
{
    public TestAuthenticationHandler(IOptionsMonitor<TestAuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        
        if (string.IsNullOrEmpty(authHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Parse test authentication header
        if (authHeader.StartsWith("Test "))
        {
            var token = authHeader.Substring("Test ".Length);
            var parts = token.Split(':');
            
            if (parts.Length >= 1 && Enum.TryParse<UserRole>(parts[0], out var role))
            {
                Guid? userId = null;
                Guid? studentId = null;

                if (parts.Length >= 2 && Guid.TryParse(parts[1], out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                if (parts.Length >= 3 && Guid.TryParse(parts[2], out var parsedStudentId))
                {
                    studentId = parsedStudentId;
                }

                var principal = TestAuthenticationHelper.CreateTestUser(role, userId, studentId);
                var ticket = new AuthenticationTicket(principal, "Test");
                
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
        }

        // Default to configured role if no specific header
        var defaultPrincipal = TestAuthenticationHelper.CreateTestUser(
            Options.DefaultRole, 
            Options.UserId, 
            Options.StudentId);
        var defaultTicket = new AuthenticationTicket(defaultPrincipal, "Test");
        
        return Task.FromResult(AuthenticateResult.Success(defaultTicket));
    }
}

public static class TestAuthenticationExtensions
{
    public static void AddTestAuthentication(this IServiceCollection services, 
        UserRole defaultRole = UserRole.Student,
        Guid? userId = null,
        Guid? studentId = null)
    {
        services.AddAuthentication("Test")
            .AddScheme<TestAuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options =>
            {
                options.DefaultRole = defaultRole;
                options.UserId = userId;
                options.StudentId = studentId;
            });
    }
}