using EduShield.Api.Auth;
using EduShield.Core.Configuration;
using EduShield.Core.Dtos;
using EduShield.Core.Interfaces;
using EduShield.Core.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly AuthCallbackHandler _callbackHandler;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserService userService,
        AuthCallbackHandler callbackHandler,
        IOptions<AuthenticationConfiguration> authConfig,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userService = userService;
        _callbackHandler = callbackHandler;
        _authConfig = authConfig.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initiate Google OAuth login
    /// </summary>
    [HttpGet("login/google")]
    public IActionResult LoginGoogle(string? returnUrl = null)
    {
        var googleConfig = _authConfig.Providers["Google"];
        var state = SecurityHelper.SanitizeUserAgent(returnUrl ?? "/");
        
        var redirectUri = googleConfig.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback/google";
        
        var authUrl = $"{googleConfig.Authority}/o/oauth2/v2/auth?" +
                     $"client_id={Uri.EscapeDataString(googleConfig.ClientId)}&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"response_type=code&" +
                     $"scope={Uri.EscapeDataString(string.Join(" ", googleConfig.Scopes))}&" +
                     $"state={Uri.EscapeDataString(state)}";

        return Redirect(authUrl);
    }

    /// <summary>
    /// Handle Google OAuth callback
    /// </summary>
    [HttpGet("callback/google")]
    public async Task<IActionResult> GoogleCallback(string code, string? state = null, string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Google OAuth error: {Error}", error);
            return BadRequest(new { error = "Authentication failed", details = error });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { error = "Authorization code is required" });
        }

        try
        {
            // For testing purposes, we'll create a mock ID token
            // In production, you'd exchange the code for tokens with Google
            var mockIdToken = CreateMockIdToken();
            
            var ipAddress = SecurityHelper.SanitizeIpAddress(HttpContext.Connection.RemoteIpAddress?.ToString());
            var userAgent = SecurityHelper.SanitizeUserAgent(Request.Headers.UserAgent);

            var result = await _callbackHandler.HandleGoogleCallbackAsync(mockIdToken, ipAddress, userAgent);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            // Set authentication cookie
            var cookieOptions = _callbackHandler.CreateAuthCookie();
            Response.Cookies.Append(_authConfig.CookieName, result.SessionToken!, cookieOptions);

            // Redirect to return URL or default
            var returnUrl = !string.IsNullOrEmpty(state) ? state : "/";
            return Redirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Google callback");
            return StatusCode(500, new { error = "Authentication processing failed" });
        }
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return BadRequest("Invalid user ID");
        }

        var profile = await _userService.GetUserProfileAsync(userId);
        if (profile == null)
        {
            return NotFound("User profile not found");
        }

        return Ok(profile);
    }

    /// <summary>
    /// Logout current user
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var sessionIdClaim = User.FindFirst("session_id")?.Value;

        if (Guid.TryParse(userIdClaim, out var userId) && !string.IsNullOrEmpty(sessionIdClaim))
        {
            await _authService.LogoutAsync(userId, sessionIdClaim);
        }

        // Clear authentication cookie
        Response.Cookies.Delete(_authConfig.CookieName);

        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Initiate Microsoft OAuth login
    /// </summary>
    [HttpGet("login/microsoft")]
    public IActionResult LoginMicrosoft(string? returnUrl = null)
    {
        var microsoftConfig = _authConfig.Providers["Microsoft"];
        var state = SecurityHelper.SanitizeUserAgent(returnUrl ?? "/");
        
        var redirectUri = microsoftConfig.RedirectUri ?? $"{Request.Scheme}://{Request.Host}/api/v1/auth/callback/microsoft";
        
        var authUrl = $"{microsoftConfig.Authority}/oauth2/v2.0/authorize?" +
                     $"client_id={Uri.EscapeDataString(microsoftConfig.ClientId)}&" +
                     $"redirect_uri={Uri.EscapeDataString(redirectUri)}&" +
                     $"response_type=code&" +
                     $"scope={Uri.EscapeDataString(string.Join(" ", microsoftConfig.Scopes))}&" +
                     $"state={Uri.EscapeDataString(state)}";

        return Redirect(authUrl);
    }

    /// <summary>
    /// Handle Microsoft OAuth callback
    /// </summary>
    [HttpGet("callback/microsoft")]
    public async Task<IActionResult> MicrosoftCallback(string code, string? state = null, string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            _logger.LogWarning("Microsoft OAuth error: {Error}", error);
            return BadRequest(new { error = "Authentication failed", details = error });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { error = "Authorization code is required" });
        }

        try
        {
            // For testing purposes, we'll create a mock ID token
            // In production, you'd exchange the code for tokens with Microsoft
            var mockIdToken = CreateMockMicrosoftIdToken();
            
            var ipAddress = SecurityHelper.SanitizeIpAddress(HttpContext.Connection.RemoteIpAddress?.ToString());
            var userAgent = SecurityHelper.SanitizeUserAgent(Request.Headers.UserAgent);

            var result = await _callbackHandler.HandleMicrosoftCallbackAsync(mockIdToken, ipAddress, userAgent);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            // Set authentication cookie
            var cookieOptions = _callbackHandler.CreateAuthCookie();
            Response.Cookies.Append(_authConfig.CookieName, result.SessionToken!, cookieOptions);

            // Redirect to return URL or default
            var returnUrl = !string.IsNullOrEmpty(state) ? state : "/";
            return Redirect(returnUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Microsoft callback");
            return StatusCode(500, new { error = "Authentication processing failed" });
        }
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateUserProfileRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest("Invalid user ID");
            }

            var updateRequest = new UpdateUserRequest
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber
            };

            var profile = await _userService.UpdateUserAsync(userId, updateRequest);
            if (profile == null)
            {
                return NotFound("User profile not found");
            }

            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { error = "Failed to update profile" });
        }
    }

    /// <summary>
    /// Get current user's active sessions
    /// </summary>
    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserSessionDto>>> GetMySessions()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest("Invalid user ID");
            }

            var sessions = await _userService.GetUserSessionsAsync(userId);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user sessions");
            return StatusCode(500, new { error = "Failed to retrieve sessions" });
        }
    }

    /// <summary>
    /// Invalidate a specific session
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    [Authorize]
    public async Task<IActionResult> InvalidateSession(string sessionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentSessionId = User.FindFirst("session_id")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest("Invalid user ID");
            }

            // Verify the session belongs to the current user
            var sessions = await _userService.GetUserSessionsAsync(userId);
            if (!sessions.Any(s => s.SessionId == sessionId))
            {
                return NotFound("Session not found");
            }

            var success = await _userService.InvalidateSessionAsync(sessionId);
            if (!success)
            {
                return NotFound("Session not found");
            }

            // If user invalidated their current session, clear the cookie
            if (sessionId == currentSessionId)
            {
                Response.Cookies.Delete(_authConfig.CookieName);
            }

            return Ok(new { message = "Session invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to invalidate session" });
        }
    }

    /// <summary>
    /// Invalidate all other sessions (keep current session active)
    /// </summary>
    [HttpDelete("sessions/others")]
    [Authorize]
    public async Task<IActionResult> InvalidateOtherSessions()
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var currentSessionId = User.FindFirst("session_id")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId) || string.IsNullOrEmpty(currentSessionId))
            {
                return BadRequest("Invalid user or session ID");
            }

            await _userService.InvalidateOtherUserSessionsAsync(userId, currentSessionId);

            return Ok(new { message = "Other sessions invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating other sessions");
            return StatusCode(500, new { error = "Failed to invalidate other sessions" });
        }
    }

    /// <summary>
    /// Test endpoint to check authentication status
    /// </summary>
    [HttpGet("status")]
    [Authorize]
    public IActionResult GetAuthStatus()
    {
        var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);
        return Ok(new { authenticated = true, claims });
    }

    /// <summary>
    /// Login with external token
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] AuthLoginRequest request)
    {
        try
        {
            var ipAddress = SecurityHelper.SanitizeIpAddress(HttpContext.Connection.RemoteIpAddress?.ToString());
            var userAgent = SecurityHelper.SanitizeUserAgent(Request.Headers.UserAgent);

            var result = await _authService.AuthenticateAsync(request.Token, request.Provider, HttpContext.RequestAborted);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            // Create session
            var session = await _authService.CreateSessionAsync(result.User!.UserId, ipAddress, userAgent, HttpContext.RequestAborted);
            result.SessionToken = session.SessionToken;
            result.ExpiresAt = session.ExpiresAt;

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { error = "Authentication failed" });
        }
    }

    /// <summary>
    /// Validate session
    /// </summary>
    [HttpPost("validate")]
    public async Task<IActionResult> ValidateSession([FromBody] ValidateSessionRequest request)
    {
        try
        {
            var isValid = await _authService.ValidateSessionAsync(request.SessionId, HttpContext.RequestAborted);
            return Ok(new { valid = isValid });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session");
            return StatusCode(500, new { error = "Session validation failed" });
        }
    }

    /// <summary>
    /// Handle auth callback
    /// </summary>
    [HttpPost("callback")]
    public async Task<ActionResult<AuthResult>> AuthCallback([FromBody] AuthCallbackRequest request)
    {
        try
        {
            var ipAddress = SecurityHelper.SanitizeIpAddress(HttpContext.Connection.RemoteIpAddress?.ToString());
            var userAgent = SecurityHelper.SanitizeUserAgent(Request.Headers.UserAgent);

            var result = await _authService.HandleCallbackAsync(request.Token, ipAddress, userAgent, HttpContext.RequestAborted);
            
            if (!result.Success)
            {
                return BadRequest(new { error = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling auth callback");
            return StatusCode(500, new { error = "Callback processing failed" });
        }
    }

    private string CreateMockIdToken()
    {
        // This is a mock implementation for testing
        // In production, you'd get the real ID token from Google
        var mockClaims = new Dictionary<string, object>
        {
            ["sub"] = "google-user-123",
            ["email"] = "test@gmail.com",
            ["given_name"] = "Test",
            ["family_name"] = "User",
            ["picture"] = "https://example.com/avatar.jpg",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        // Create a simple JWT-like token for testing
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(mockClaims)));
        
        return $"{header}.{payload}.";
    }

    private string CreateMockMicrosoftIdToken()
    {
        // This is a mock implementation for testing
        // In production, you'd get the real ID token from Microsoft
        var mockClaims = new Dictionary<string, object>
        {
            ["sub"] = "microsoft-user-456",
            ["email"] = "test@outlook.com",
            ["given_name"] = "Test",
            ["family_name"] = "User",
            ["picture"] = "https://example.com/avatar.jpg",
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["exp"] = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds()
        };

        // Create a simple JWT-like token for testing
        var header = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"alg\":\"none\",\"typ\":\"JWT\"}"));
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(mockClaims)));
        
        return $"{header}.{payload}.";
    }
}

public class UpdateUserProfileRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}

public class AuthLoginRequest
{
    public string Token { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
}

public class ValidateSessionRequest
{
    public string SessionId { get; set; } = string.Empty;
}

public class AuthCallbackRequest
{
    public string Token { get; set; } = string.Empty;
}