using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IStudentRepo
{
    Task<Student> CreateAsync(Student student, CancellationToken cancellationToken);
    Task<Student?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Student>> GetAllAsync(CancellationToken cancellationToken);
    Task<Student> UpdateAsync(Student student, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
