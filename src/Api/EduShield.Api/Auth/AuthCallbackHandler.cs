using EduShield.Core.Configuration;
using EduShield.Core.Dtos;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using EduShield.Core.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace EduShield.Api.Auth;

public class AuthCallbackHandler
{
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly ILogger<AuthCallbackHandler> _logger;

    public AuthCallbackHandler(
        IAuthService authService,
        IAuditService auditService,
        IOptions<AuthenticationConfiguration> authConfig,
        ILogger<AuthCallbackHandler> logger)
    {
        _authService = authService;
        _auditService = auditService;
        _authConfig = authConfig.Value;
        _logger = logger;
    }

    public async Task<AuthResult> HandleGoogleCallbackAsync(string idToken, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate and parse the ID token
            var userInfo = await ValidateGoogleTokenAsync(idToken, cancellationToken);
            if (userInfo == null)
            {
                await _auditService.LogAuthenticationAsync(null, "GoogleCallback", false, "Invalid ID token", ipAddress, userAgent, cancellationToken);
                return new AuthResult { Success = false, ErrorMessage = "Invalid token" };
            }

            // Get or create user
            var user = await _authService.GetOrCreateUserAsync(userInfo, cancellationToken);
            
            // Validate user is active
            if (!await _authService.ValidateUserAsync(user.UserId, cancellationToken))
            {
                await _auditService.LogAuthenticationAsync(user.UserId, "GoogleCallback", false, "User account is deactivated", ipAddress, userAgent, cancellationToken);
                return new AuthResult { Success = false, ErrorMessage = "Account is deactivated" };
            }

            // Create session
            var session = await _authService.CreateSessionAsync(user.UserId, ipAddress, userAgent, cancellationToken);

            // Log successful authentication
            await _auditService.LogAuthenticationAsync(user.UserId, "GoogleCallback", true, null, ipAddress, userAgent, cancellationToken);

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
            _logger.LogError(ex, "Error handling Google callback");
            await _auditService.LogAuthenticationAsync(null, "GoogleCallback", false, ex.Message, ipAddress, userAgent, cancellationToken);
            return new AuthResult { Success = false, ErrorMessage = "Authentication failed" };
        }
    }

    private async Task<ExternalUserInfo?> ValidateGoogleTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // For development, we'll do basic parsing without full validation
            // In production, you should validate the token signature against Google's public keys
            var jsonToken = handler.ReadJwtToken(idToken);

            // Basic validation
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token has expired");
                return null;
            }

            // Extract user information
            var userInfo = new ExternalUserInfo
            {
                ExternalId = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                FirstName = jsonToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty,
                LastName = jsonToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty,
                ProfilePictureUrl = jsonToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value,
                Provider = AuthProvider.Google,
                Claims = jsonToken.Claims.ToDictionary(c => c.Type, c => c.Value)
            };

            // Validate required fields
            if (string.IsNullOrEmpty(userInfo.ExternalId) || string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogWarning("Token missing required claims");
                return null;
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Google token");
            return null;
        }
    }

    public async Task<AuthResult> HandleMicrosoftCallbackAsync(string idToken, string ipAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate and parse the ID token
            var userInfo = await ValidateMicrosoftTokenAsync(idToken, cancellationToken);
            if (userInfo == null)
            {
                await _auditService.LogAuthenticationAsync(null, "MicrosoftCallback", false, "Invalid ID token", ipAddress, userAgent, cancellationToken);
                return new AuthResult { Success = false, ErrorMessage = "Invalid token" };
            }

            // Get or create user
            var user = await _authService.GetOrCreateUserAsync(userInfo, cancellationToken);
            
            // Validate user is active
            if (!await _authService.ValidateUserAsync(user.UserId, cancellationToken))
            {
                await _auditService.LogAuthenticationAsync(user.UserId, "MicrosoftCallback", false, "User account is deactivated", ipAddress, userAgent, cancellationToken);
                return new AuthResult { Success = false, ErrorMessage = "Account is deactivated" };
            }

            // Create session
            var session = await _authService.CreateSessionAsync(user.UserId, ipAddress, userAgent, cancellationToken);

            // Log successful authentication
            await _auditService.LogAuthenticationAsync(user.UserId, "MicrosoftCallback", true, null, ipAddress, userAgent, cancellationToken);

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
            _logger.LogError(ex, "Error handling Microsoft callback");
            await _auditService.LogAuthenticationAsync(null, "MicrosoftCallback", false, ex.Message, ipAddress, userAgent, cancellationToken);
            return new AuthResult { Success = false, ErrorMessage = "Authentication failed" };
        }
    }

    private async Task<ExternalUserInfo?> ValidateMicrosoftTokenAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            
            // For development, we'll do basic parsing without full validation
            // In production, you should validate the token signature against Microsoft's public keys
            var jsonToken = handler.ReadJwtToken(idToken);

            // Basic validation
            if (jsonToken.ValidTo < DateTime.UtcNow)
            {
                _logger.LogWarning("Token has expired");
                return null;
            }

            // Extract user information
            var userInfo = new ExternalUserInfo
            {
                ExternalId = jsonToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty,
                Email = jsonToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
                FirstName = jsonToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? string.Empty,
                LastName = jsonToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? string.Empty,
                ProfilePictureUrl = jsonToken.Claims.FirstOrDefault(c => c.Type == "picture")?.Value,
                Provider = AuthProvider.Microsoft,
                Claims = jsonToken.Claims.ToDictionary(c => c.Type, c => c.Value)
            };

            // Validate required fields
            if (string.IsNullOrEmpty(userInfo.ExternalId) || string.IsNullOrEmpty(userInfo.Email))
            {
                _logger.LogWarning("Token missing required claims");
                return null;
            }

            return userInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating Microsoft token");
            return null;
        }
    }

    public CookieOptions CreateAuthCookie()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = _authConfig.RequireSecureCookies,
            SameSite = SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.Add(_authConfig.SessionTimeout),
            Path = "/"
        };
    }
}