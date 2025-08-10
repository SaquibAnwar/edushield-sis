using EduShield.Core.Dtos;

namespace EduShield.Core.Interfaces;

public interface IStudentService
{
    Task<Guid> CreateAsync(CreateStudentReq request, CancellationToken cancellationToken);
    Task<StudentDto?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<StudentDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<bool> UpdateAsync(Guid id, CreateStudentReq request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
