using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class FacultyRepo : IFacultyRepo
{
    private readonly EduShieldDbContext _context;

    public FacultyRepo(EduShieldDbContext context)
    {
        _context = context;
    }

    public async Task<Faculty> CreateAsync(Faculty faculty, CancellationToken cancellationToken)
    {
        _context.Faculty.Add(faculty);
        await _context.SaveChangesAsync(cancellationToken);
        return faculty;
    }

    public async Task<Faculty?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Faculty
            .Include(f => f.Students)
            .Include(f => f.Performances)
            .FirstOrDefaultAsync(f => f.FacultyId == id, cancellationToken);
    }

    public async Task<IEnumerable<Faculty>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Faculty
            .Include(f => f.Students)
            .Include(f => f.Performances)
            .ToListAsync(cancellationToken);
    }

    public async Task<Faculty> UpdateAsync(Faculty faculty, CancellationToken cancellationToken)
    {
        _context.Faculty.Update(faculty);
        await _context.SaveChangesAsync(cancellationToken);
        return faculty;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var faculty = await _context.Faculty.FindAsync(id, cancellationToken);
        if (faculty != null)
        {
            _context.Faculty.Remove(faculty);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
