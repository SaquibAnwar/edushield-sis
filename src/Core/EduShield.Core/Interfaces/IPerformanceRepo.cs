using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IPerformanceRepo : IBaseRepository<Performance>
{
    Task<IEnumerable<Performance>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Performance>> GetByFacultyIdAsync(Guid facultyId, CancellationToken cancellationToken = default);
}