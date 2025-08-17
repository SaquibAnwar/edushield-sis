using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class SessionRepo : ISessionRepo
{
    private readonly EduShieldDbContext _context;

    public SessionRepo(EduShieldDbContext context)
    {
        _context = context;
    }

    public async Task<UserSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionId == sessionId, cancellationToken);
    }

    public async Task<UserSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.SessionToken == sessionToken, cancellationToken);
    }

    public async Task<UserSession> CreateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _context.UserSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<UserSession> UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
    {
        _context.UserSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task<bool> DeleteAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetByIdAsync(sessionId, cancellationToken);
        if (session == null) return false;

        _context.UserSessions.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<UserSession>> GetByUserIdAsync(Guid userId, bool activeOnly = true, CancellationToken cancellationToken = default)
    {
        var query = _context.UserSessions
            .Where(s => s.UserId == userId);

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive && s.ExpiresAt > DateTime.UtcNow);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserSessions
            .Where(s => s.IsActive && s.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await GetByUserIdAsync(userId, true, cancellationToken);
    }
}