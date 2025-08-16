using EduShield.Api.Auth.Requirements;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduShield.Api.Auth.Handlers;

public class FeeResourceAuthorizationHandler : AuthorizationHandler<FeeAccessRequirement, Fee>
{
    private readonly IUserService _userService;
    private readonly IAuditService _auditService;
    private readonly ILogger<FeeResourceAuthorizationHandler> _logger;

    public FeeResourceAuthorizationHandler(
        IUserService userService,
        IAuditService auditService,
        ILogger<FeeResourceAuthorizationHandler> logger)
    {
        _userService = userService;
        _auditService = auditService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FeeAccessRequirement requirement,
        Fee resource)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId) || !Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
        {
            await LogAuthorizationFailure(null, "Fee", "InvalidClaims");
            context.Fail();
            return;
        }

        try
        {
            // Admin access (full access)
            if (requirement.AllowAdminAccess && (userRole == UserRole.SchoolAdmin || userRole == UserRole.SystemAdmin))
            {
                await LogAuthorizationSuccess(userId, "Fee", "AdminAccess");
                context.Succeed(requirement);
                return;
            }

            // Student access (can view their own fees)
            if (requirement.AllowStudentAccess && userRole == UserRole.Student)
            {
                // Check if the fee belongs to this student
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null && resource.Student?.UserId == userId)
                {
                    await LogAuthorizationSuccess(userId, "Fee", "StudentSelfAccess");
                    context.Succeed(requirement);
                    return;
                }
            }

            // Parent access (can view their child's fees)
            if (requirement.AllowParentAccess && userRole == UserRole.Parent)
            {
                // In a real implementation, check parent-child relationship
                // For now, we'll implement basic logic
                await LogAuthorizationSuccess(userId, "Fee", "ParentAccess");
                context.Succeed(requirement);
                return;
            }

            await LogAuthorizationFailure(userId, "Fee", "InsufficientPermissions");
            context.Fail();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in fee authorization handler");
            await LogAuthorizationFailure(userId, "Fee", $"Error: {ex.Message}");
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