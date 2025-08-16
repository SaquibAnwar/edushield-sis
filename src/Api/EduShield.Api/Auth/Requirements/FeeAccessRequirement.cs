using Microsoft.AspNetCore.Authorization;

namespace EduShield.Api.Auth.Requirements;

public class FeeAccessRequirement : IAuthorizationRequirement
{
    public bool AllowStudentAccess { get; set; } = true;
    public bool AllowParentAccess { get; set; } = true;
    public bool AllowAdminAccess { get; set; } = true;
    public bool ReadOnly { get; set; } = false;
}