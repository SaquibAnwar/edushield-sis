using Microsoft.AspNetCore.Authorization;

namespace EduShield.Api.Auth.Requirements;

public class FacultyAccessRequirement : IAuthorizationRequirement
{
    public bool AllowSelfAccess { get; set; } = true;
    public bool AllowAdminAccess { get; set; } = true;
}