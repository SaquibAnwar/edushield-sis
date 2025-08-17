using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EduShield.Api.Controllers;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = "SchoolAdminOnly")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ISessionService _sessionService;
    private readonly IAuditService _auditService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserService userService,
        ISessionService sessionService,
        IAuditService auditService,
        ILogger<UserController> logger)
    {
        _userService = userService;
        _sessionService = sessionService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var users = await _userService.GetUsersAsync(page, pageSize, role, isActive);
            
            await _auditService.LogAsync(
                "UserAccess",
                "Retrieved user list",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { page, pageSize, role, isActive }));

            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { error = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> GetUser(Guid id)
    {
        try
        {
            var user = await _userService.GetUserProfileAsync(id);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            await _auditService.LogAsync(
                "UserAccess",
                "Retrieved user profile",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id }));

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve user" });
        }
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserProfileDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.CreateUserAsync(request);
            
            await _auditService.LogAsync(
                "UserCreation",
                "Created new user",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { newUserId = user.UserId, email = user.Email, role = user.Role }));

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { error = "Failed to create user" });
        }
    }

    /// <summary>
    /// Update user information
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserProfileDto>> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userService.UpdateUserAsync(id, request);
            if (user == null)
            {
                return NotFound(new { error = "User not found" });
            }

            await _auditService.LogAsync(
                "UserUpdate",
                "Updated user information",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id, changes = request }));

            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new { error = "Failed to update user" });
        }
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid id)
    {
        try
        {
            var success = await _userService.DeactivateUserAsync(id);
            if (!success)
            {
                return NotFound(new { error = "User not found" });
            }

            // Invalidate all user sessions
            await _sessionService.InvalidateUserSessionsAsync(id);

            await _auditService.LogAsync(
                "UserDeactivation",
                "Deactivated user account",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id }));

            return Ok(new { message = "User deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating user {UserId}", id);
            return StatusCode(500, new { error = "Failed to deactivate user" });
        }
    }

    /// <summary>
    /// Reactivate user account
    /// </summary>
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> ActivateUser(Guid id)
    {
        try
        {
            var success = await _userService.ActivateUserAsync(id);
            if (!success)
            {
                return NotFound(new { error = "User not found" });
            }

            await _auditService.LogAsync(
                "UserActivation",
                "Activated user account",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id }));

            return Ok(new { message = "User activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating user {UserId}", id);
            return StatusCode(500, new { error = "Failed to activate user" });
        }
    }

    /// <summary>
    /// Update user role
    /// </summary>
    [HttpPost("{id:guid}/role")]
    [Authorize(Policy = "SystemAdminOnly")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateUserRoleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _userService.UpdateUserRoleAsync(id, request.Role);
            if (!success)
            {
                return NotFound(new { error = "User not found" });
            }

            await _auditService.LogAsync(
                "RoleChange",
                "Updated user role",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id, newRole = request.Role }));

            return Ok(new { message = "User role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role {UserId}", id);
            return StatusCode(500, new { error = "Failed to update user role" });
        }
    }

    /// <summary>
    /// Get user's active sessions
    /// </summary>
    [HttpGet("{id:guid}/sessions")]
    public async Task<ActionResult<IEnumerable<UserSessionDto>>> GetUserSessions(Guid id)
    {
        try
        {
            var sessions = await _sessionService.GetUserSessionsAsync(id);
            
            await _auditService.LogAsync(
                "SessionAccess",
                "Retrieved user sessions",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id }));

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve user sessions" });
        }
    }

    /// <summary>
    /// Invalidate specific user session
    /// </summary>
    [HttpDelete("{id:guid}/sessions/{sessionId}")]
    public async Task<IActionResult> InvalidateUserSession(Guid id, string sessionId)
    {
        try
        {
            var success = await _sessionService.InvalidateSessionAsync(sessionId);
            if (!success)
            {
                return NotFound(new { error = "Session not found" });
            }

            await _auditService.LogAsync(
                "SessionInvalidation",
                "Invalidated user session",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id, sessionId }));

            return Ok(new { message = "Session invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating session {SessionId} for user {UserId}", sessionId, id);
            return StatusCode(500, new { error = "Failed to invalidate session" });
        }
    }

    /// <summary>
    /// Invalidate all user sessions
    /// </summary>
    [HttpDelete("{id:guid}/sessions")]
    public async Task<IActionResult> InvalidateAllUserSessions(Guid id)
    {
        try
        {
            await _sessionService.InvalidateUserSessionsAsync(id);

            await _auditService.LogAsync(
                "SessionInvalidation",
                "Invalidated all user sessions",
                GetCurrentUserId(),
                true,
                null,
                System.Text.Json.JsonSerializer.Serialize(new { targetUserId = id }));

            return Ok(new { message = "All user sessions invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating all sessions for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to invalidate user sessions" });
        }
    }

    /// <summary>
    /// Get all users (without pagination)
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetAllUsers()
    {
        try
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { error = "Failed to retrieve users" });
        }
    }

    /// <summary>
    /// Get users by role
    /// </summary>
    [HttpGet("role/{role}")]
    public async Task<ActionResult<IEnumerable<UserProfileDto>>> GetUsersByRole(UserRole role)
    {
        try
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users by role {Role}", role);
            return StatusCode(500, new { error = "Failed to retrieve users by role" });
        }
    }

    /// <summary>
    /// Invalidate user sessions
    /// </summary>
    [HttpPost("{id:guid}/sessions/invalidate")]
    public async Task<IActionResult> InvalidateUserSessions(Guid id)
    {
        try
        {
            await _sessionService.InvalidateAllUserSessionsAsync(id);
            return Ok(new { message = "User sessions invalidated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating user sessions {UserId}", id);
            return StatusCode(500, new { error = "Failed to invalidate user sessions" });
        }
    }

    /// <summary>
    /// Get user audit logs
    /// </summary>
    [HttpGet("{id:guid}/audit")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetUserAuditLogs(Guid id)
    {
        try
        {
            var logs = await _auditService.GetUserAuditLogsAsync(id);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs for user {UserId}", id);
            return StatusCode(500, new { error = "Failed to retrieve audit logs" });
        }
    }

    /// <summary>
    /// Get all audit logs
    /// </summary>
    [HttpGet("audit")]
    public async Task<ActionResult<IEnumerable<AuditLog>>> GetAllAuditLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? action = null)
    {
        try
        {
            var logs = await _auditService.GetAuditLogsAsync(userId, action);
            return Ok(logs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { error = "Failed to retrieve audit logs" });
        }
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

public class UpdateUserRoleRequest
{
    public UserRole Role { get; set; }
}