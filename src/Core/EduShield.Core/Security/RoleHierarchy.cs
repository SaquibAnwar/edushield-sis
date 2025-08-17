using EduShield.Core.Enums;

namespace EduShield.Core.Security;

public static class RoleHierarchy
{
    private static readonly Dictionary<UserRole, int> RoleHierarchyLevels = new()
    {
        { UserRole.Student, 1 },
        { UserRole.Parent, 2 },
        { UserRole.Teacher, 3 },
        { UserRole.SchoolAdmin, 4 },
        { UserRole.SystemAdmin, 5 }
    };

    public static bool HasPermission(UserRole userRole, UserRole requiredRole)
    {
        return GetRoleLevel(userRole) >= GetRoleLevel(requiredRole);
    }

    public static bool IsAdmin(UserRole userRole)
    {
        return userRole == UserRole.SchoolAdmin || userRole == UserRole.SystemAdmin;
    }

    public static bool IsTeacherOrAdmin(UserRole userRole)
    {
        return userRole == UserRole.Teacher || IsAdmin(userRole);
    }

    public static bool CanAccessStudentData(UserRole userRole)
    {
        return userRole == UserRole.Student || userRole == UserRole.Parent || IsTeacherOrAdmin(userRole);
    }

    public static bool CanModifyStudentData(UserRole userRole)
    {
        return IsTeacherOrAdmin(userRole);
    }

    public static bool CanAccessFeeData(UserRole userRole)
    {
        return userRole == UserRole.Student || userRole == UserRole.Parent || IsAdmin(userRole);
    }

    public static bool CanModifyFeeData(UserRole userRole)
    {
        return IsAdmin(userRole);
    }

    public static bool CanAccessPerformanceData(UserRole userRole)
    {
        return CanAccessStudentData(userRole);
    }

    public static bool CanModifyPerformanceData(UserRole userRole)
    {
        return IsTeacherOrAdmin(userRole);
    }

    public static UserRole[] GetSubordinateRoles(UserRole userRole)
    {
        var userLevel = GetRoleLevel(userRole);
        return RoleHierarchyLevels
            .Where(kvp => kvp.Value < userLevel)
            .Select(kvp => kvp.Key)
            .ToArray();
    }

    public static UserRole[] GetEqualOrSubordinateRoles(UserRole userRole)
    {
        var userLevel = GetRoleLevel(userRole);
        return RoleHierarchyLevels
            .Where(kvp => kvp.Value <= userLevel)
            .Select(kvp => kvp.Key)
            .ToArray();
    }

    public static int GetHierarchyLevel(UserRole role)
    {
        return GetRoleLevel(role);
    }

    private static int GetRoleLevel(UserRole role)
    {
        return RoleHierarchyLevels.TryGetValue(role, out var level) ? level : 0;
    }
}