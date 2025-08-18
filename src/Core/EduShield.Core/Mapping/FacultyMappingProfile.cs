using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class FacultyMappingProfile : Profile
{
    public FacultyMappingProfile()
    {
        CreateMap<Faculty, FacultyDto>();
        CreateMap<CreateFacultyReq, Faculty>()
            .ForMember(dest => dest.FacultyId, opt => opt.Ignore())
            .ForMember(dest => dest.Performances, opt => opt.Ignore())
            .ForMember(dest => dest.Students, opt => opt.Ignore());
    }
}

