using EduShield.Core.Dtos;

namespace EduShield.Core.Interfaces;

public interface IPerformanceService
{
    Task<Guid> CreateAsync(CreatePerformanceReq request, CancellationToken cancellationToken = default);
    Task<PerformanceDto?> GetAsync(Guid performanceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceDto>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PerformanceDto>> GetByFacultyIdAsync(Guid facultyId, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(Guid performanceId, CreatePerformanceReq request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid performanceId, CancellationToken cancellationToken = default);
}