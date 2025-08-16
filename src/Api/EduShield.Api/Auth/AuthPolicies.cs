namespace EduShield.Api.Auth;

public static class AuthPolicies
{
    public const string SchoolAdminOnly = "SchoolAdminOnly";
    public const string SystemAdminOnly = "SystemAdminOnly";
    public const string TeacherOrAdmin = "TeacherOrAdmin";
    public const string StudentOwnerOrAdmin = "StudentOwnerOrAdmin";
    public const string ParentOrAdmin = "ParentOrAdmin";
    public const string TeacherStudentOrAdmin = "TeacherStudentOrAdmin";
    public const string SelfOrAdmin = "SelfOrAdmin";
    public const string FeeAccess = "FeeAccess";
    public const string PerformanceAccess = "PerformanceAccess";
}