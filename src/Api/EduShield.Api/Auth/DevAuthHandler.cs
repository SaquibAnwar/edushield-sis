using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EduShield.Core.Interfaces;
using EduShield.Core.Dtos;
using EduShield.Core.Enums;

namespace EduShield.Api.Auth;

public class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IServiceProvider _serviceProvider;
    private static string? _devUserId;

    public DevAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IServiceProvider serviceProvider)
        : base(options, logger, encoder)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Ensure dev user exists
        if (_devUserId == null)
        {
            _devUserId = await EnsureDevUserExistsAsync();
        }

        // In dev/test, always authenticate successfully with SchoolAdmin role
        // This allows testing without requiring authentication headers
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, _devUserId),
            new Claim(ClaimTypes.Name, "Dev User"),
            new Claim(ClaimTypes.Email, "dev@edushield.local"),
            new Claim(ClaimTypes.Role, "SchoolAdmin"),
            new Claim("role", "SchoolAdmin"),
            new Claim("sub", _devUserId)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return AuthenticateResult.Success(ticket);
    }

    private async Task<string> EnsureDevUserExistsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

        try
        {
            // Try to find existing dev user
            var existingUser = await userService.GetUserByEmailAsync("dev@edushield.local");
            if (existingUser != null)
            {
                return existingUser.UserId.ToString();
            }

            // Create dev user if it doesn't exist
            var createRequest = new CreateUserRequest
            {
                Email = "dev@edushield.local",
                FirstName = "Dev",
                LastName = "User",
                ExternalId = "dev-user-123",
                Provider = AuthProvider.Custom,
                Role = UserRole.SchoolAdmin
            };

            var newUser = await userService.CreateUserAsync(createRequest);
            return newUser.UserId.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to ensure dev user exists");
            // Return a default GUID if user creation fails
            return Guid.NewGuid().ToString();
        }
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        // In development, don't challenge - just authenticate automatically
        return Task.CompletedTask;
    }

    protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        // In development, return proper forbidden response
        Response.StatusCode = 403;
        return Response.WriteAsync("Access denied");
    }
}
