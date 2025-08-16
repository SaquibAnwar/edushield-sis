using EduShield.Api.Auth.Requirements;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduShield.Api.Auth.Handlers;

public class PerformanceResourceAuthorizationHandler : AuthorizationHandler<PerformanceAccessRequirement, Performance>
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<PerformanceResourceAuthorizationHandler> _logger;

    public PerformanceResourceAuthorizationHandler(
        IUserService userService,
        IAuditService auditService,
        ILogger<PerformanceResourceAuthorizationHandler> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PerformanceAccessRequirement requirement,
        Performance resource)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId) || !Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
        {
            await LogAuthorizationFailure(null, "Performance", "InvalidClaims");
            context.Fail();
            return;
        }

        try
        {
            // Admin access (full access)
            if (requirement.AllowAdminAccess && (userRole == UserRole.SchoolAdmin || userRole == UserRole.SystemAdmin))
            {
                await LogAuthorizationSuccess(userId, "Performance", "AdminAccess");
                context.Succeed(requirement);
                return;
            }

            // Teacher access (can manage performance for their students)
            if (requirement.AllowTeacherAccess && userRole == UserRole.Teacher)
            {
                // Check if the teacher is assigned to this student or subject
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    // In a real implementation, check teacher-student/subject relationships
                    await LogAuthorizationSuccess(userId, "Performance", "TeacherAccess");
                    context.Succeed(requirement);
                    return;
                }
            }

            // Student access (can view their own performance)
            if (requirement.AllowStudentAccess && userRole == UserRole.Student)
            {
                // Check if the performance record belongs to this student
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null && resource.Student?.UserId == userId)
                {
                    await LogAuthorizationSuccess(userId, "Performance", "StudentSelfAccess");
                    context.Succeed(requirement);
                    return;
                }
            }

            // Parent access (can view their child's performance)
            if (requirement.AllowParentAccess && userRole == UserRole.Parent)
            {
                // In a real implementation, check parent-child relationship
                await LogAuthorizationSuccess(userId, "Performance", "ParentAccess");
                context.Succeed(requirement);
                return;
            }

            await LogAuthorizationFailure(userId, "Performance", "InsufficientPermissions");
            context.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in performance authorization handler");
            await LogAuthorizationFailure(userId, "Performance", $"Error: {ex.Message}");
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