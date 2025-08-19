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

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Sessions)
            .ToListAsync(cancellationToken);
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

    public async Task<bool> UpdateAsync(Guid id, User entity, CancellationToken cancellationToken)
    {
        var existingUser = await _context.Users.FindAsync(id, cancellationToken);
        if (existingUser == null)
            return false;

        // Update properties - only update properties that actually exist
        existingUser.Email = entity.Email;
        existingUser.FirstName = entity.FirstName;
        existingUser.LastName = entity.LastName;
        existingUser.PhoneNumber = entity.PhoneNumber;
        existingUser.ExternalId = entity.ExternalId;
        existingUser.Provider = entity.Provider;
        existingUser.Role = entity.Role;
        existingUser.IsActive = entity.IsActive;
        existingUser.LastLoginAt = entity.LastLoginAt;
        existingUser.ProfilePictureUrl = entity.ProfilePictureUrl;

        _context.Users.Update(existingUser);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
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