using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;
using EduShield.Core.Interfaces;
using EduShield.Core.Mapping;
using Microsoft.EntityFrameworkCore;

namespace EduShield.Api.Services;

public class StudentService : IStudentService
{
    private readonly IStudentRepo _studentRepo;
    private readonly IMapper _mapper;

    public StudentService(IStudentRepo studentRepo, IMapper mapper)
    {
        _studentRepo = studentRepo;
        _mapper = mapper;
    }

    public async Task<Guid> CreateAsync(CreateStudentReq request, CancellationToken cancellationToken)
    {
        var student = _mapper.Map<Student>(request);
        student.Id = Guid.NewGuid();
        student.CreatedAt = DateTime.UtcNow;
        student.UpdatedAt = DateTime.UtcNow;

        await _studentRepo.CreateAsync(student, cancellationToken);
        return student.Id;
    }

    public async Task<StudentDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var student = await _studentRepo.GetByIdAsync(id, cancellationToken);
        return student != null ? _mapper.Map<StudentDto>(student) : null;
    }

    public async Task<IEnumerable<StudentDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var students = await _studentRepo.GetAllAsync(cancellationToken);
        return _mapper.Map<IEnumerable<StudentDto>>(students);
    }

    public async Task<bool> UpdateAsync(Guid id, CreateStudentReq request, CancellationToken cancellationToken)
    {
        var existingStudent = await _studentRepo.GetByIdAsync(id, cancellationToken);
        if (existingStudent == null)
        {
            return false;
        }

        // Update properties
        existingStudent.FirstName = request.FirstName;
        existingStudent.LastName = request.LastName;
        existingStudent.Email = request.Email;
        existingStudent.PhoneNumber = request.PhoneNumber;
        existingStudent.DateOfBirth = DateTime.SpecifyKind(request.DateOfBirth, DateTimeKind.Utc);
        existingStudent.Gender = request.Gender;
        existingStudent.Address = request.Address;
        existingStudent.EnrollmentDate = DateTime.SpecifyKind(request.EnrollmentDate, DateTimeKind.Utc);
        existingStudent.UpdatedAt = DateTime.UtcNow;

        return await _studentRepo.UpdateAsync(id, existingStudent, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var existingStudent = await _studentRepo.GetByIdAsync(id, cancellationToken);
        if (existingStudent == null)
        {
            return false;
        }

        await _studentRepo.DeleteAsync(id, cancellationToken);
        return true;
    }
}
