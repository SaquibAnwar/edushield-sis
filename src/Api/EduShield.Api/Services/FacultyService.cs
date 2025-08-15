using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Services;

public class FacultyService : IFacultyService
{
    private readonly IFacultyRepo _facultyRepo;
    private readonly IMapper _mapper;

    public FacultyService(IFacultyRepo facultyRepo, IMapper mapper)
    {
        _facultyRepo = facultyRepo;
        _mapper = mapper;
    }

    public async Task<Guid> CreateAsync(CreateFacultyReq request, CancellationToken cancellationToken)
    {
        var faculty = _mapper.Map<Faculty>(request);
        faculty.FacultyId = Guid.NewGuid();
        
        var createdFaculty = await _facultyRepo.CreateAsync(faculty, cancellationToken);
        return createdFaculty.FacultyId;
    }

    public async Task<FacultyDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var faculty = await _facultyRepo.GetByIdAsync(id, cancellationToken);
        return faculty != null ? _mapper.Map<FacultyDto>(faculty) : null;
    }

    public async Task<IEnumerable<FacultyDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var faculties = await _facultyRepo.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<FacultyDto>>(faculties);
    }

    public async Task<bool> UpdateAsync(Guid id, CreateFacultyReq request, CancellationToken cancellationToken)
    {
        var existingFaculty = await _facultyRepo.GetByIdAsync(id, cancellationToken);
        if (existingFaculty == null)
            return false;

        existingFaculty.Name = request.Name;
        existingFaculty.Department = request.Department;
        existingFaculty.Subject = request.Subject;
        existingFaculty.Gender = request.Gender;

        await _facultyRepo.UpdateAsync(existingFaculty, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existingFaculty = await _facultyRepo.GetByIdAsync(id, cancellationToken);
        if (existingFaculty == null)
            return false;

        await _facultyRepo.DeleteAsync(id, cancellationToken);
        return true;
    }
}
