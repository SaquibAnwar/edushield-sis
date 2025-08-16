using Microsoft.AspNetCore.Authorization;

namespace EduShield.Api.Auth.Requirements;

public class StudentAccessRequirement : IAuthorizationRequirement
{
    public bool AllowSelfAccess { get; set; } = true;
    public bool AllowTeacherAccess { get; set; } = true;
    public bool AllowParentAccess { get; set; } = false;
    public bool AllowAdminAccess { get; set; } = true;
}