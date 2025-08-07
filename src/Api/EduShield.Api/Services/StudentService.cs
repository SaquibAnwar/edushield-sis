using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using FluentValidation;

namespace EduShield.Api.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepo _studentRepo;
    private readonly IValidator<CreateStudentReq> _validator;
    private readonly IMapper _mapper;
    private readonly ILogger<StudentService> _logger;

    public StudentService(
        IStudentRepo studentRepo,
        IValidator<CreateStudentReq> validator,
        IMapper mapper,
        ILogger<StudentService> logger)
    {
        _studentRepo = studentRepo;
        _validator = validator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Guid> CreateAsync(CreateStudentReq req, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new student with name: {Name}", req.Name);

        // Validate the request
        var validationResult = await _validator.ValidateAsync(req, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed for student creation: {Errors}", errors);
            throw new ArgumentException($"Validation failed: {errors}");
        }

        try
        {
            // Map request to entity
            var student = _mapper.Map<Student>(req);

            // Persist to database
            var createdStudent = await _studentRepo.CreateAsync(student, cancellationToken);

            _logger.LogInformation("Successfully created student with ID: {StudentId}", createdStudent.StudentId);
            return createdStudent.StudentId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create student with name: {Name}", req.Name);
            throw new ApplicationException("Failed to create student", ex);
        }
    }

    public async Task<StudentDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving student with ID: {StudentId}", id);

        try
        {
            var student = await _studentRepo.GetByIdAsync(id, cancellationToken);
            if (student == null)
            {
                _logger.LogWarning("Student not found with ID: {StudentId}", id);
                return null;
            }

            var dto = _mapper.Map<StudentDto>(student);
            _logger.LogInformation("Successfully retrieved student with ID: {StudentId}", id);
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve student with ID: {StudentId}", id);
            throw new ApplicationException("Failed to retrieve student", ex);
        }
    }
}
