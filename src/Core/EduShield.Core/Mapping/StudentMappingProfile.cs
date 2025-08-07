using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class StudentMappingProfile : Profile
{
    public StudentMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<Student, StudentDto>();

        // Request to Entity mapping
        CreateMap<CreateStudentReq, Student>()
            .ConstructUsing(src => new Student(
                Guid.NewGuid(),
                src.Name,
                src.Class,
                src.Section,
                src.Gender));
    }
}
