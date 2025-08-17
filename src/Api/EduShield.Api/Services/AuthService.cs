using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EduShield.Api.Services;

public class AuthService : IAuthService
{
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserService userService,
        ISessionService sessionService,
        IAuditService auditService,
        ILogger<AuthService> logger)
    {
        _userService = userService;
        _sessionService = sessionService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<AuthResult> AuthenticateAsync(string token, string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            // Parse and validate the JWT token
            var userInfo = await ExtractUserInfoFromTokenAsync(token, provider, cancellationToken);
            if (userInfo == null)
            {
                await _auditService.LogAuthenticationAsync(null, "TokenValidation", false, "Invalid token format", cancellationToken: cancellationToken);
                return new AuthResult { Success = false, ErrorMessage = "Invalid token" };
            }

            // Get or create user
            var user = await GetOrCreateUserAsync(userInfo, cancellationToken);
            
            // Validate user is active
            if (!await ValidateUserAsync(user.UserId, cancellationToken))
            {
                await _auditService.LogAuthenticationAsync(user.UserId, "Login", false, "User account is deactivated", cancellationToken: cancellationToken);
                return new AuthResult { Success = false, ErrorMessage = "Account is deactivated" };
            }

            // Update last login
            await _userService.UpdateLastLoginAsync(user.UserId, cancellationToken);

            // Log successful authentication
            await _auditService.LogAuthenticationAsync(user.UserId, "Login", true, cancellationToken: cancellationToken);

            return new AuthResult
            {
                Success = true,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Provider = user.Provider,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    LastLoginAt = user.LastLoginAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Authentication failed for provider {Provider}", provider);
            await _auditService.LogAuthenticationAsync(null, "Login", false, ex.Message, cancellationToken: cancellationToken);
            return new AuthResult { Success = false, ErrorMessage = "Authentication failed" };
        }
    }

    public async Task<User> GetOrCreateUserAsync(ExternalUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        // Try to find existing user by external ID
        var existingUser = await _userService.GetUserByExternalIdAsync(userInfo.ExternalId, userInfo.Provider, cancellationToken);
        if (existingUser != null)
        {
            return existingUser;
        }

        // Try to find by email (user might have changed provider)
        existingUser = await _userService.GetUserByEmailAsync(userInfo.Email, cancellationToken);
        if (existingUser != null)
        {
            // Update external ID for this provider
            var updateRequest = new UpdateUserRequest
            {
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                ProfilePictureUrl = userInfo.ProfilePictureUrl
            };
            await _userService.UpdateUserAsync(existingUser.UserId, updateRequest, cancellationToken);
            return existingUser;
        }

        // Create new user
        var createRequest = new CreateUserRequest
        {
            Email = userInfo.Email,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            ExternalId = userInfo.ExternalId,
            Provider = userInfo.Provider,
            Role = UserRole.Student, // Default role
            ProfilePictureUrl = userInfo.ProfilePictureUrl
        };

        var newUser = await _userService.CreateUserAsync(createRequest, cancellationToken);
        await _auditService.LogAsync("UserCreated", "User", newUser.UserId, true, cancellationToken: cancellationToken);
        
        return newUser;
    }

    public async Task<bool> ValidateUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userService.GetUserByIdAsync(userId, cancellationToken);
        return user?.IsActive == true;
    }

    public async Task LogoutAsync(Guid userId, string sessionId, CancellationToken cancellationToken = default)
    {
        await InvalidateSessionAsync(sessionId, cancellationToken);
        await _auditService.LogAuthenticationAsync(userId, "Logout", true, cancellationToken: cancellationToken);
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        var session = await _sessionService.CreateSessionAsync(userId, ipAddress, userAgent, cancellationToken: cancellationToken);
        await _auditService.LogAsync("SessionCreated", "UserSession", userId, true, additionalData: $"SessionId: {session.SessionId}", ipAddress: ipAddress, userAgent: userAgent, cancellationToken: cancellationToken);
        return session;
    }

    public async Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        await _sessionService.InvalidateSessionAsync(sessionId, cancellationToken);
        
        if (session != null)
        {
            await _auditService.LogAsync("SessionInvalidated", "UserSession", session.UserId, true, additionalData: $"SessionId: {sessionId}", cancellationToken: cancellationToken);
        }
    }

    public async Task<bool> IsSessionValidAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await _sessionService.ValidateSessionAsync(sessionId, cancellationToken);
    }

    public async Task InvalidateAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await _sessionService.InvalidateUserSessionsAsync(userId, cancellationToken);
        await _auditService.LogAsync("AllSessionsInvalidated", "UserSession", userId, true, cancellationToken: cancellationToken);
    }

    public async Task<AuthResult> ValidateExternalTokenAsync(string token, string provider, CancellationToken cancellationToken = default)
    {
        return await AuthenticateAsync(token, provider, cancellationToken);
    }

    public async Task<AuthResult> HandleCallbackAsync(string token, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        try
        {
            var userInfo = await ExtractUserInfoFromTokenAsync(token, "Google", cancellationToken);
            if (userInfo == null)
            {
                return new AuthResult { Success = false, ErrorMessage = "Invalid token" };
            }

            var user = await GetOrCreateUserAsync(userInfo, cancellationToken);
            var session = await CreateSessionAsync(user.UserId, ipAddress, userAgent, cancellationToken);

            return new AuthResult
            {
                Success = true,
                User = new UserDto
                {
                    UserId = user.UserId,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    FullName = user.FullName,
                    Provider = user.Provider,
                    Role = user.Role,
                    IsActive = user.IsActive,
                    LastLoginAt = user.LastLoginAt,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                },
                SessionToken = session.SessionToken,
                ExpiresAt = session.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling callback");
            return new AuthResult { Success = false, ErrorMessage = "Authentication failed" };
        }
    }

    public async Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        return await IsSessionValidAsync(sessionId, cancellationToken);
    }

    private async Task<ExternalUserInfo?> ExtractUserInfoFromTokenAsync(string token, string provider, CancellationToken cancellationToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadJwtToken(token);

            var providerEnum = Enum.Parse<AuthProvider>(provider, true);
            
            return new ExternalUserInfo
            {
                ExternalId = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                FirstName = jsonToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty,
                LastName = jsonToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty,
                ProfilePictureUrl = jsonToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value,
                Provider = providerEnum,
                Claims = jsonToken.Claims.ToDictionary(c => c.Type, c => c.Value)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract user info from token");
            return null;
        }
    }
}