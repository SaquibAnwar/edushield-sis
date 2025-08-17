using EduShield.Api.Auth.Requirements;
using EduShield.Core.Dtos;
using EduShield.Core.Enums;
using EduShield.Core.Security;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace EduShield.Api.Auth.Handlers;

public class FacultyResourceAuthorizationHandler : AuthorizationHandler<FacultyAccessRequirement, FacultyDto>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FacultyAccessRequirement requirement,
        FacultyDto faculty)
    {
        var userRole = GetUserRole(context.User);
        var userId = GetUserId(context.User);

        // System and School Admins have full access
        if (userRole == UserRole.SystemAdmin || userRole == UserRole.SchoolAdmin)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Teachers can access their own faculty record
        if (userRole == UserRole.Teacher && requirement.AllowSelfAccess)
        {
            if (faculty.UserId.HasValue && faculty.UserId == userId)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }

        // Teachers can view other faculty members (for collaboration)
        if (userRole == UserRole.Teacher)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Students can view faculty information (read-only)
        if (userRole == UserRole.Student)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        return Task.CompletedTask;
    }

    private static UserRole GetUserRole(ClaimsPrincipal user)
    {
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;
        return Enum.TryParse<UserRole>(roleClaim, out var role) ? role : UserRole.Student;
    }

    private static Guid? GetUserId(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}