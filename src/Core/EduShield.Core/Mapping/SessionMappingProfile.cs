using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class SessionMappingProfile : Profile
{
    public SessionMappingProfile()
    {
        CreateMap<UserSession, UserSessionDto>()
            .ForMember(dest => dest.SessionId, opt => opt.MapFrom(src => src.SessionToken));
    }
}