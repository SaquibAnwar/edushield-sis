using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Enums;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class UserRepo : IUserRepo
{
    private readonly EduShieldDbContext _context;

    public UserRepo(EduShieldDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByExternalIdAsync(string externalId, AuthProvider provider, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.ExternalId == externalId && u.Provider == provider, cancellationToken);
    }

    public async Task<User> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<User> UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
        return user;
    }

    public async Task<bool> DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await GetByIdAsync(userId, cancellationToken);
        if (user == null) return false;

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IEnumerable<User>> GetAllAsync(UserRole? role = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();
        
        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize, UserRole? role = null, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();
        
        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<Student?> GetStudentByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<Faculty?> GetFacultyByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Faculty
            .FirstOrDefaultAsync(f => f.UserId == userId, cancellationToken);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Where(u => u.Role == role)
            .ToListAsync(cancellationToken);
    }
}