using EduShield.Core.Dtos;

namespace EduShield.Core.Interfaces;

public interface IFacultyService
{
    Task<Guid> CreateAsync(CreateFacultyReq request, CancellationToken cancellationToken);
    Task<FacultyDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<FacultyDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid id, CreateFacultyReq request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}

