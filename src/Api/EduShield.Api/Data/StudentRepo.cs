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

    public async Task<bool> UpdateAsync(Guid id, Student entity, CancellationToken cancellationToken)
    {
        var existingStudent = await _context.Students.FindAsync(id, cancellationToken);
        if (existingStudent == null)
            return false;

        // Update properties - only update properties that actually exist
        existingStudent.FirstName = entity.FirstName;
        existingStudent.LastName = entity.LastName;
        existingStudent.Email = entity.Email;
        existingStudent.PhoneNumber = entity.PhoneNumber;
        existingStudent.DateOfBirth = entity.DateOfBirth;
        existingStudent.Address = entity.Address;
        existingStudent.EnrollmentDate = entity.EnrollmentDate;
        existingStudent.Gender = entity.Gender;
        existingStudent.FacultyId = entity.FacultyId;

        _context.Students.Update(existingStudent);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var student = await _context.Students.FindAsync(id, cancellationToken);
        if (student == null)
            return false;

        _context.Students.Remove(student);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
