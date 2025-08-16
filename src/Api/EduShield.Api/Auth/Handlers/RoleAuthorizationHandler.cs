using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduShield.Api.Auth.Handlers;

public class RoleAuthorizationHandler : IAuthorizationHandler
{
    private readonly IAuditService _auditService;
    private readonly ILogger<RoleAuthorizationHandler> _logger;

    public RoleAuthorizationHandler(IAuditService auditService, ILogger<RoleAuthorizationHandler> logger)
    {
        _auditService = auditService;
        _logger = logger;
    }

    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Guid.TryParse(userIdClaim, out var userId) || !Enum.TryParse<UserRole>(userRoleClaim, out var userRole))
        {
            return;
        }

        foreach (var requirement in context.Requirements)
        {
            if (requirement is RoleRequirement roleRequirement)
            {
                await HandleRoleRequirement(context, roleRequirement, userId, userRole);
            }
        }
    }

    private async Task HandleRoleRequirement(AuthorizationHandlerContext context, RoleRequirement requirement, Guid userId, UserRole userRole)
    {
        try
        {
            var hasRequiredRole = requirement.AllowedRoles.Contains(userRole);
            
            if (hasRequiredRole)
            {
                await _auditService.LogAuthorizationAsync(userId, "Role", "Check", true, $"Role: {userRole}");
                context.Succeed(requirement);
            }
            else
            {
                await _auditService.LogAuthorizationAsync(userId, "Role", "Check", false, $"Required: {string.Join(",", requirement.AllowedRoles)}, Actual: {userRole}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in role authorization handler");
            await _auditService.LogAuthorizationAsync(userId, "Role", "Check", false, $"Error: {ex.Message}");
        }
    }
}

public class RoleRequirement : IAuthorizationRequirement
{
    public UserRole[] AllowedRoles { get; }

    public RoleRequirement(params UserRole[] allowedRoles)
    {
        AllowedRoles = allowedRoles ?? throw new ArgumentNullException(nameof(allowedRoles));
    }
}