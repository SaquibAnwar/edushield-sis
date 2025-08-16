using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class AuditRepo : IAuditRepo
{
    private readonly EduShieldDbContext _context;

    public AuditRepo(EduShieldDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog> CreateAsync(AuditLog auditLog, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(auditLog);
        await _context.SaveChangesAsync(cancellationToken);
        return auditLog;
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(Guid? userId = null, string? action = null, DateTime? fromDate = null, DateTime? toDate = null, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(l => l.Action == action);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteOldLogsAsync(DateTime cutoffDate, CancellationToken cancellationToken = default)
    {
        var oldLogs = await _context.AuditLogs
            .Where(l => l.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        _context.AuditLogs.RemoveRange(oldLogs);
        await _context.SaveChangesAsync(cancellationToken);
        
        return oldLogs.Count;
    }
}