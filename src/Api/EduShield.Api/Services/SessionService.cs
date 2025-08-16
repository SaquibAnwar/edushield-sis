using EduShield.Core.Configuration;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace EduShield.Api.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepo _sessionRepo;
    private readonly AuthenticationConfiguration _authConfig;
    private readonly ILogger<SessionService> _logger;

    public SessionService(
        ISessionRepo sessionRepo,
        IOptions<AuthenticationConfiguration> authConfig,
        ILogger<SessionService> logger)
    {
        _sessionRepo = sessionRepo;
        _authConfig = authConfig.Value;
        _logger = logger;
    }

    public async Task<UserSession> CreateSessionAsync(Guid userId, string ipAddress, string userAgent, TimeSpan? customTimeout = null, CancellationToken cancellationToken = default)
    {
        // Invalidate existing sessions if multiple sessions not allowed
        if (!_authConfig.AllowMultipleSessions)
        {
            await InvalidateAllUserSessionsAsync(userId, cancellationToken);
        }

        var sessionTimeout = customTimeout ?? _authConfig.SessionTimeout;
        var sessionToken = GenerateSecureToken();

        var session = new UserSession
        {
            SessionId = Guid.NewGuid(),
            UserId = userId,
            SessionToken = sessionToken,
            ExpiresAt = DateTime.UtcNow.Add(sessionTimeout),
            IpAddress = ipAddress,
            UserAgent = userAgent,
            IsActive = true
        };

        return await _sessionRepo.CreateAsync(session, cancellationToken);
    }

    public async Task<UserSession?> GetSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return await _sessionRepo.GetByTokenAsync(sessionToken, cancellationToken);
    }

    public async Task<bool> ValidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepo.GetByTokenAsync(sessionToken, cancellationToken);
        return session?.IsValid == true;
    }

    public async Task InvalidateSessionAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepo.GetByTokenAsync(sessionToken, cancellationToken);
        if (session != null)
        {
            session.IsActive = false;
            await _sessionRepo.UpdateAsync(session, cancellationToken);
        }
    }

    public async Task InvalidateAllUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _sessionRepo.GetByUserIdAsync(userId, activeOnly: true, cancellationToken);
        foreach (var session in sessions)
        {
            session.IsActive = false;
            await _sessionRepo.UpdateAsync(session, cancellationToken);
        }
    }

    public async Task<IEnumerable<UserSession>> GetUserSessionsAsync(Guid userId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        return await _sessionRepo.GetByUserIdAsync(userId, activeOnly, cancellationToken);
    }

    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredSessions = await _sessionRepo.GetExpiredSessionsAsync(cancellationToken);
            foreach (var session in expiredSessions)
            {
                session.IsActive = false;
                await _sessionRepo.UpdateAsync(session, cancellationToken);
            }
            
            _logger.LogInformation("Cleaned up {Count} expired sessions", expiredSessions.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during session cleanup");
        }
    }

    public async Task ExtendSessionAsync(string sessionToken, TimeSpan extension, CancellationToken cancellationToken = default)
    {
        var session = await _sessionRepo.GetByTokenAsync(sessionToken, cancellationToken);
        if (session?.IsValid == true)
        {
            session.ExpiresAt = session.ExpiresAt.Add(extension);
            await _sessionRepo.UpdateAsync(session, cancellationToken);
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}