using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IPerformanceRepo
{
    Task<Performance> CreateAsync(Performance performance, CancellationToken cancellationToken = default);
    Task<Performance?> GetByIdAsync(Guid performanceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Performance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Performance>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Performance>> GetByFacultyIdAsync(Guid facultyId, CancellationToken cancellationToken = default);
    Task<Performance?> UpdateAsync(Performance performance, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid performanceId, CancellationToken cancellationToken = default);
}