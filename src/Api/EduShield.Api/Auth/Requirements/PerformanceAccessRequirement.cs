using Microsoft.AspNetCore.Authorization;

namespace EduShield.Api.Auth.Requirements;

public class PerformanceAccessRequirement : IAuthorizationRequirement
{
    public bool AllowStudentAccess { get; set; } = true;
    public bool AllowTeacherAccess { get; set; } = true;
    public bool AllowParentAccess { get; set; } = true;
    public bool AllowAdminAccess { get; set; } = true;
    public bool ReadOnly { get; set; } = false;
}