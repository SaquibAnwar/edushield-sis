using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class StudentMappingProfile : Profile
{
    public StudentMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<Student, StudentDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
            .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
            .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => src.EnrollmentDate))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Gender))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.FacultyId, opt => opt.MapFrom(src => src.FacultyId));

        // Request to Entity mapping
        CreateMap<CreateStudentReq, Student>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.DateOfBirth, DateTimeKind.Utc)))
            .ForMember(dest => dest.EnrollmentDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.EnrollmentDate, DateTimeKind.Utc)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}
