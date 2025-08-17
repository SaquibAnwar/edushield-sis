using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Enums;

namespace EduShield.Core.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetUserByExternalIdAsync(string externalId, AuthProvider provider, CancellationToken cancellationToken = default);
    Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ActivateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> AssignRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
    Task<bool> UpdateUserRoleAsync(Guid userId, UserRole role, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfileDto>> GetUsersAsync(int page = 1, int pageSize = 20, UserRole? role = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> GetUserProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task UpdateLastLoginAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task InvalidateOtherUserSessionsAsync(Guid userId, string currentSessionId, CancellationToken cancellationToken = default);
    Task<User> CreateFromExternalAsync(ExternalUserInfo userInfo, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<UserProfileDto?> UpdateAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfileDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<UserProfileDto>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}