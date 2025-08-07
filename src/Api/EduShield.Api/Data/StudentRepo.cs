using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class StudentRepo : IStudentRepo
{
    private readonly EduShieldDbContext _context;

    public StudentRepo(EduShieldDbContext context)
    {
        _context = context;
    }

    public async Task<Student> CreateAsync(Student student, CancellationToken cancellationToken = default)
    {
        _context.Students.Add(student);
        await _context.SaveChangesAsync(cancellationToken);
        return student;
    }

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.StudentId == id, cancellationToken);
    }

    public async Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .ToListAsync(cancellationToken);
    }

    public async Task<Student> UpdateAsync(Student student, CancellationToken cancellationToken = default)
    {
        _context.Entry(student).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
        return student;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var student = await GetByIdAsync(id, cancellationToken);
        if (student == null)
            return false;

        _context.Students.Remove(student);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .AnyAsync(s => s.StudentId == id, cancellationToken);
    }
}
