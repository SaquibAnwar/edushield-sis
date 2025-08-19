using EduShield.Core.Data;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Data;

public class PerformanceRepo : IPerformanceRepo
{
    private readonly EduShieldDbContext _context;

    public PerformanceRepo(EduShieldDbContext context)
    {
        _context = context;
    }

    public async Task<Performance> CreateAsync(Performance performance, CancellationToken cancellationToken = default)
    {
        _context.Performances.Add(performance);
        await _context.SaveChangesAsync(cancellationToken);
        return performance;
    }

    public async Task<Performance?> GetByIdAsync(Guid performanceId, CancellationToken cancellationToken = default)
    {
        return await _context.Performances
            .Include(p => p.Student)
            .Include(p => p.Faculty)
            .FirstOrDefaultAsync(p => p.PerformanceId == performanceId, cancellationToken);
    }

    public async Task<IEnumerable<Performance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Performances
            .Include(p => p.Student)
            .Include(p => p.Faculty)
            .OrderByDescending(p => p.ExamDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Performance>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        return await _context.Performances
            .Include(p => p.Student)
            .Include(p => p.Faculty)
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.ExamDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Performance>> GetByFacultyIdAsync(Guid facultyId, CancellationToken cancellationToken = default)
    {
        return await _context.Performances
            .Include(p => p.Student)
            .Include(p => p.Faculty)
            .Where(p => p.FacultyId == facultyId)
            .OrderByDescending(p => p.ExamDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Guid id, Performance entity, CancellationToken cancellationToken = default)
    {
        var existingPerformance = await _context.Performances
            .FirstOrDefaultAsync(p => p.PerformanceId == id, cancellationToken);

        if (existingPerformance == null)
            return false;

        existingPerformance.StudentId = entity.StudentId;
        existingPerformance.FacultyId = entity.FacultyId;
        existingPerformance.Subject = entity.Subject;
        existingPerformance.Marks = entity.Marks;
        existingPerformance.MaxMarks = entity.MaxMarks;
        existingPerformance.ExamDate = entity.ExamDate;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid performanceId, CancellationToken cancellationToken = default)
    {
        var performance = await _context.Performances
            .FirstOrDefaultAsync(p => p.PerformanceId == performanceId, cancellationToken);

        if (performance == null)
            return false;

        _context.Performances.Remove(performance);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}