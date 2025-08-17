using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> AuthenticateAsync(string token, string provider, CancellationToken cancellationToken = default);
    Task<User> GetOrCreateUserAsync(ExternalUserInfo userInfo, CancellationToken cancellationToken = default);
    Task<bool> ValidateUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task LogoutAsync(Guid userId, string sessionId, CancellationToken cancellationToken = default);
    Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task InvalidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<bool> IsSessionValidAsync(string sessionId, CancellationToken cancellationToken = default);
    Task InvalidateAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AuthResult> ValidateExternalTokenAsync(string token, string provider, CancellationToken cancellationToken = default);
    Task<AuthResult> HandleCallbackAsync(string token, string ipAddress, string userAgent, CancellationToken cancellationToken = default);
    Task<bool> ValidateSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}