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

    public async Task<bool> UpdateAsync(Guid id, Faculty entity, CancellationToken cancellationToken)
    {
        var existingFaculty = await _context.Faculty.FindAsync(id, cancellationToken);
        if (existingFaculty == null)
            return false;

        // Update properties - only update properties that actually exist
        existingFaculty.Name = entity.Name;
        existingFaculty.Department = entity.Department;
        existingFaculty.Subject = entity.Subject;
        existingFaculty.Gender = entity.Gender;

        _context.Faculty.Update(existingFaculty);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var faculty = await _context.Faculty.FindAsync(id, cancellationToken);
        if (faculty == null)
            return false;

        _context.Faculty.Remove(faculty);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
