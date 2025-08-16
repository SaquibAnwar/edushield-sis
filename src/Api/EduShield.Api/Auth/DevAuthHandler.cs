using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EduShield.Api.Auth;

public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // In dev/test, always authenticate successfully with SchoolAdmin role
        // This allows testing without requiring authentication headers
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-user-123"),
            new Claim(ClaimTypes.Name, "Dev User"),
            new Claim(ClaimTypes.Email, "dev@edushield.local"),
            new Claim(ClaimTypes.Role, "SchoolAdmin"),
            new Claim("role", "SchoolAdmin"),
            new Claim("sub", "dev-user-123")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // In development, don't challenge - just authenticate automatically
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // In development, don't forbid - just authenticate automatically
        return Task.CompletedTask;
    }
}
