using EduShield.Core.Dtos;

namespace EduShield.Core.Interfaces;

public interface IStudentService
{
    Task<Guid> CreateAsync(CreateStudentReq req, CancellationToken cancellationToken = default);
    Task<StudentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
