using EduShield.Api.Auth.Requirements;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduShield.Api.Auth.Handlers;

public class StudentResourceAuthorizationHandler : AuthorizationHandler<StudentAccessRequirement, Student>
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<StudentResourceAuthorizationHandler> _logger;

    public StudentResourceAuthorizationHandler(
        IUserService userService,
        IAuditService auditService,
        ILogger<StudentResourceAuthorizationHandler> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        StudentAccessRequirement requirement,
        Student resource)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Failed to parse user ID: {UserIdClaim}", userIdClaim);
            await LogAuthorizationFailure(null, "Student", "InvalidUserId");
            context.Fail();
            return;
        }

        if (!Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
        {
            _logger.LogWarning("Failed to parse user role: {UserRoleClaim}", userRoleClaim);
            await LogAuthorizationFailure(userId, "Student", "InvalidRole");
            context.Fail();
            return;
        }

        _logger.LogInformation("Authorization check for user {UserId} with role {UserRole}", userId, userRole);

        try
        {
            // Admin access
            if (requirement.AllowAdminAccess && (userRole == UserRole.SchoolAdmin || userRole == UserRole.SystemAdmin))
            {
                await LogAuthorizationSuccess(userId, "Student", "AdminAccess");
                context.Succeed(requirement);
                return;
            }

            // Self access (student accessing their own record)
            if (requirement.AllowSelfAccess && userRole == UserRole.Student && resource.UserId == userId)
            {
                await LogAuthorizationSuccess(userId, "Student", "SelfAccess");
                context.Succeed(requirement);
                return;
            }

            // Teacher access (teacher accessing their student's record)
            if (requirement.AllowTeacherAccess && userRole == UserRole.Teacher)
            {
                // Check if the teacher is assigned to this student
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    // In a real implementation, you'd check if the teacher is assigned to this student
                    // For now, we'll allow all teachers to access all students
                    await LogAuthorizationSuccess(userId, "Student", "TeacherAccess");
                    context.Succeed(requirement);
                    return;
                }
            }

            // Parent access (parent accessing their child's record)
            if (requirement.AllowParentAccess && userRole == UserRole.Parent)
            {
                // In a real implementation, you'd check parent-child relationships
                // For now, we'll implement basic logic
                await LogAuthorizationSuccess(userId, "Student", "ParentAccess");
                context.Succeed(requirement);
                return;
            }

            await LogAuthorizationFailure(userId, "Student", "InsufficientPermissions");
            context.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in student authorization handler");
            await LogAuthorizationFailure(userId, "Student", $"Error: {ex.Message}");
            context.Fail();
        }
    }

    private async Task LogAuthorizationSuccess(Guid userId, string resource, string reason)
    {
        await _auditService.LogAuthorizationAsync(userId, resource, "Access", true, reason);
    }

    private async Task LogAuthorizationFailure(Guid? userId, string resource, string reason)
    {
        await _auditService.LogAuthorizationAsync(userId ?? Guid.Empty, resource, "Access", false, reason);
    }
}