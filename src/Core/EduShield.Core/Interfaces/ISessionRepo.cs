using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface ISessionRepo
{
    Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<UserSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<UserSession> UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetByUserIdAsync(Guid userId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);
}