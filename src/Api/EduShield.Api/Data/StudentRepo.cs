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

    public async Task<Student> CreateAsync(Student student, CancellationToken cancellationToken)
    {
        _context.Students.Add(student);
        await _context.SaveChangesAsync(cancellationToken);
        return student;
    }

    public async Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Students
            .Include(s => s.Faculty)
            .Include(s => s.Performances)
            .Include(s => s.Fees)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Students
            .Include(s => s.Faculty)
            .Include(s => s.Performances)
            .Include(s => s.Fees)
            .ToListAsync(cancellationToken);
    }

    public async Task<Student> UpdateAsync(Student student, CancellationToken cancellationToken)
    {
        _context.Students.Update(student);
        await _context.SaveChangesAsync(cancellationToken);
        return student;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FindAsync(new object[] { id }, cancellationToken);
        if (student != null)
        {
            _context.Students.Remove(student);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
