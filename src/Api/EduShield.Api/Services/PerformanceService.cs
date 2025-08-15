using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;

namespace EduShield.Api.Services;

public class PerformanceService : IPerformanceService
{
    private readonly IPerformanceRepo _performanceRepo;
    private readonly IStudentRepo _studentRepo;
    private readonly IFacultyRepo _facultyRepo;
    private readonly IMapper _mapper;
    private readonly ILogger<PerformanceService> _logger;

    public PerformanceService(
        IPerformanceRepo performanceRepo,
        IStudentRepo studentRepo,
        IFacultyRepo facultyRepo,
        IMapper mapper,
        ILogger<PerformanceService> logger)
    {
        _performanceRepo = performanceRepo;
        _studentRepo = studentRepo;
        _facultyRepo = facultyRepo;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(CreatePerformanceReq request, CancellationToken cancellationToken = default)
    {
        // Validate that student exists
        var student = await _studentRepo.GetByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            throw new ArgumentException($"Student with ID {request.StudentId} not found");
        }

        // Validate that faculty exists
        var faculty = await _facultyRepo.GetByIdAsync(request.FacultyId, cancellationToken);
        if (faculty == null)
        {
            throw new ArgumentException($"Faculty with ID {request.FacultyId} not found");
        }

        var performance = _mapper.Map<Performance>(request);
        var createdPerformance = await _performanceRepo.CreateAsync(performance, cancellationToken);

        _logger.LogInformation("Performance record created with ID: {PerformanceId} for Student: {StudentId}", 
            createdPerformance.PerformanceId, request.StudentId);

        return createdPerformance.PerformanceId;
    }

    public async Task<PerformanceDto?> GetAsync(Guid performanceId, CancellationToken cancellationToken = default)
    {
        var performance = await _performanceRepo.GetByIdAsync(performanceId, cancellationToken);
        return performance == null ? null : _mapper.Map<PerformanceDto>(performance);
    }

    public async Task<IEnumerable<PerformanceDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var performances = await _performanceRepo.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<PerformanceDto>>(performances);
    }

    public async Task<IEnumerable<PerformanceDto>> GetByStudentIdAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var performances = await _performanceRepo.GetByStudentIdAsync(studentId, cancellationToken);
        return _mapper.Map<IEnumerable<PerformanceDto>>(performances);
    }

    public async Task<IEnumerable<PerformanceDto>> GetByFacultyIdAsync(Guid facultyId, CancellationToken cancellationToken = default)
    {
        var performances = await _performanceRepo.GetByFacultyIdAsync(facultyId, cancellationToken);
        return _mapper.Map<IEnumerable<PerformanceDto>>(performances);
    }

    public async Task<bool> UpdateAsync(Guid performanceId, CreatePerformanceReq request, CancellationToken cancellationToken = default)
    {
        // Validate that student exists
        var student = await _studentRepo.GetByIdAsync(request.StudentId, cancellationToken);
        if (student == null)
        {
            throw new ArgumentException($"Student with ID {request.StudentId} not found");
        }

        // Validate that faculty exists
        var faculty = await _facultyRepo.GetByIdAsync(request.FacultyId, cancellationToken);
        if (faculty == null)
        {
            throw new ArgumentException($"Faculty with ID {request.FacultyId} not found");
        }

        var performance = _mapper.Map<Performance>(request);
        // Create new performance with the specified ID
        performance = new Performance(
            performanceId,
            performance.StudentId,
            performance.FacultyId,
            performance.Subject,
            performance.Marks,
            performance.MaxMarks,
            performance.ExamDate
        );

        var updatedPerformance = await _performanceRepo.UpdateAsync(performance, cancellationToken);
        
        if (updatedPerformance != null)
        {
            _logger.LogInformation("Performance record updated with ID: {PerformanceId}", performanceId);
            return true;
        }

        return false;
    }

    public async Task<bool> DeleteAsync(Guid performanceId, CancellationToken cancellationToken = default)
    {
        var result = await _performanceRepo.DeleteAsync(performanceId, cancellationToken);
        
        if (result)
        {
            _logger.LogInformation("Performance record deleted with ID: {PerformanceId}", performanceId);
        }

        return result;
    }
}