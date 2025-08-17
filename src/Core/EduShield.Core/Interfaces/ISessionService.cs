using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface ISessionService
{
    Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, TimeSpan? customTimeout = null, CancellationToken cancellationToken = default);
    Task<UserSession?> GetSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task<bool> InvalidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default);
    Task InvalidateUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSessionDto>> GetUserSessionsAsync(Guid userId, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default);
    Task ExtendSessionAsync(string sessionToken, TimeSpan extension, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSessionDto>> GetActiveSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task InvalidateAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}