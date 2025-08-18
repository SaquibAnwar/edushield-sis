using EduShield.Core.Entities;

namespace EduShield.Core.Interfaces;

public interface IFacultyRepo
{
    Task<Faculty> CreateAsync(Faculty faculty, CancellationToken cancellationToken);
    Task<Faculty?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IEnumerable<Faculty>> GetAllAsync(CancellationToken cancellationToken);
    Task<Faculty> UpdateAsync(Faculty faculty, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

