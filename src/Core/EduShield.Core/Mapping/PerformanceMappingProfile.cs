using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class PerformanceMappingProfile : Profile
{
    public PerformanceMappingProfile()
    {
        CreateMap<CreatePerformanceReq, Performance>()
            .ForMember(dest => dest.PerformanceId, opt => opt.MapFrom(src => Guid.NewGuid()));

        CreateMap<Performance, PerformanceDto>()
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => 
                src.Student != null ? $"{src.Student.FirstName} {src.Student.LastName}" : null))
            .ForMember(dest => dest.FacultyName, opt => opt.MapFrom(src => 
                src.Faculty != null ? src.Faculty.Name : null));
    }
}