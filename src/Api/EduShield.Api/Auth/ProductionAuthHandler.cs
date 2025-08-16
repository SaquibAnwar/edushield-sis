using EduShield.Core.Configuration;
using EduShield.Core.Interfaces;
using EduShield.Core.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EduShield.Api.Auth;

public class ProductionAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly AuthenticationConfiguration _authConfig;

    public ProductionAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISessionService sessionService,
        IAuditService auditService,
        IOptions<AuthenticationConfiguration> authConfig)
        : base(options, logger, encoder)
    {
        _sessionService = sessionService;
        _auditService = auditService;
        _authConfig = authConfig.Value;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Development bypass
        if (_authConfig.EnableDevelopmentBypass && Context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            return await HandleDevelopmentAuth();
        }

        // Check for session token in cookie
        var sessionToken = Request.Cookies[_authConfig.CookieName];
        if (string.IsNullOrEmpty(sessionToken))
        {
            return AuthenticateResult.NoResult();
        }

        try
        {
            // Validate session
            var session = await _sessionService.GetSessionAsync(sessionToken);
            if (session?.IsValid != true || session.User == null)
            {
                await LogFailedAuth("Invalid or expired session");
                return AuthenticateResult.Fail("Invalid session");
            }

            // Create claims principal
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, session.UserId.ToString()),
                new(ClaimTypes.Name, session.User.FullName),
                new(ClaimTypes.Email, session.User.Email),
                new(ClaimTypes.Role, session.User.Role.ToString()),
                new("session_id", session.SessionId.ToString()),
                new("session_token", sessionToken),
                new("provider", session.User.Provider.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Authentication error");
            await LogFailedAuth($"Authentication error: {ex.Message}");
            return AuthenticateResult.Fail("Authentication failed");
        }
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // Redirect to login page or return 401
        if (Request.Path.StartsWithSegments("/api"))
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";
            await Response.WriteAsJsonAsync(new { error = "Authentication required" });
        }
        else
        {
            // Redirect to Google OAuth
            var redirectUrl = $"/api/v1/auth/login/google?returnUrl={Uri.EscapeDataString(Request.Path + Request.QueryString)}";
            Response.Redirect(redirectUrl);
        }
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        await LogFailedAuth("Access forbidden");
        
        Response.StatusCode = 403;
        Response.ContentType = "application/json";
        await Response.WriteAsJsonAsync(new { error = "Access forbidden" });
    }

    private async Task<AuthenticateResult> HandleDevelopmentAuth()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
            new Claim(ClaimTypes.Name, "Development User"),
            new Claim(ClaimTypes.Email, "dev@edushield.local"),
            new Claim(ClaimTypes.Role, "SchoolAdmin"),
            new Claim("session_id", Guid.NewGuid().ToString()),
            new Claim("session_token", "dev-session-token"),
            new Claim("provider", "Development")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task LogFailedAuth(string reason)
    {
        var ipAddress = SecurityHelper.SanitizeIpAddress(Context.Connection.RemoteIpAddress?.ToString());
        var userAgent = SecurityHelper.SanitizeUserAgent(Request.Headers.UserAgent);
        
        await _auditService.LogAuthenticationAsync(null, "Authentication", false, reason, ipAddress, userAgent);
    }
}