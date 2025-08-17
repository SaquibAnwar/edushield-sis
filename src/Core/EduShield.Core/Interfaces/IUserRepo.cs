using EduShield.Core.Entities;
using EduShield.Core.Enums;

namespace EduShield.Core.Interfaces;

public interface IUserRepo
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByExternalIdAsync(string externalId, AuthProvider provider, CancellationToken cancellationToken = default);
    Task<User> CreateAsync(User user, CancellationToken cancellationToken = default);
    Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(UserRole? role = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, UserRole? role = null, bool? isActive = null, CancellationToken cancellationToken = default);
    Task<Student?> GetStudentByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Faculty?> GetFacultyByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
}