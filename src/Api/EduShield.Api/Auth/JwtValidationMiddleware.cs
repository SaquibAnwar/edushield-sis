using EduShield.Core.Configuration;
using EduShield.Core.Interfaces;
using EduShield.Core.Security;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace EduShield.Api.Auth;

public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(
        RequestDelegate next,
        IOptions<AuthenticationConfiguration> authConfig,
        ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _authConfig = authConfig.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISessionService sessionService, IAuditService auditService)
    {
        // Skip authentication for certain paths
        if (ShouldSkipAuthentication(context.Request.Path))
        {
            await _next(context);
            return;
        }

        // Development bypass
        if (_authConfig.EnableDevelopmentBypass && context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            await SetDevelopmentUser(context);
            await _next(context);
            return;
        }

        // Check for session token in cookie
        var sessionToken = context.Request.Cookies[_authConfig.CookieName];
        if (string.IsNullOrEmpty(sessionToken))
        {
            await HandleUnauthorized(context, auditService, "No session token found");
            return;
        }

        // Validate session
        var isValidSession = await sessionService.ValidateSessionAsync(sessionToken);
        if (!isValidSession)
        {
            await HandleUnauthorized(context, auditService, "Invalid or expired session");
            return;
        }

        // Get session details
        var session = await sessionService.GetSessionAsync(sessionToken);
        if (session?.User == null)
        {
            await HandleUnauthorized(context, auditService, "Session user not found");
            return;
        }

        // Set user context
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
            new(ClaimTypes.Name, session.User.FullName),
            new(ClaimTypes.Email, session.User.Email),
            new(ClaimTypes.Role, session.User.Role.ToString()),
            new("session_id", session.SessionId.ToString()),
            new("session_token", sessionToken)
        };

        var identity = new ClaimsIdentity(claims, "Session");
        context.User = new ClaimsPrincipal(identity);

        await _next(context);
    }

    private static bool ShouldSkipAuthentication(PathString path)
    {
        var skipPaths = new[]
        {
            "/api/v1/health",
            "/api/v1/auth/login",
            "/api/v1/auth/callback",
            "/swagger",
            "/favicon.ico"
        };

        return skipPaths.Any(skipPath => path.StartsWithSegments(skipPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task SetDevelopmentUser(HttpContext context)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "dev-user-123"),
            new(ClaimTypes.Name, "Development User"),
            new(ClaimTypes.Email, "dev@edushield.local"),
            new(ClaimTypes.Role, "SchoolAdmin"),
            new("session_id", Guid.NewGuid().ToString()),
            new("session_token", "dev-session-token")
        };

        var identity = new ClaimsIdentity(claims, "Development");
        context.User = new ClaimsPrincipal(identity);
    }

    private async Task HandleUnauthorized(HttpContext context, IAuditService auditService, string reason)
    {
        var ipAddress = SecurityHelper.SanitizeIpAddress(context.Connection.RemoteIpAddress?.ToString());
        var userAgent = SecurityHelper.SanitizeUserAgent(context.Request.Headers.UserAgent);

        await auditService.LogAuthenticationAsync(null, "Unauthorized", false, reason, ipAddress, userAgent);

        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        
        var response = new { error = "Unauthorized", message = "Authentication required" };
        await context.Response.WriteAsJsonAsync(response);
    }
}