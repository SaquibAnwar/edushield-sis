using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class FeeMappingProfile : Profile
{
    public FeeMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<Fee, FeeDto>()
            .ForMember(dest => dest.FeeId, opt => opt.MapFrom(src => src.FeeId))
            .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
            .ForMember(dest => dest.FeeType, opt => opt.MapFrom(src => src.FeeType))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.PaidAmount, opt => opt.MapFrom(src => src.PaidAmount))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.IsPaid, opt => opt.MapFrom(src => src.IsPaid))
            .ForMember(dest => dest.PaidDate, opt => opt.MapFrom(src => src.PaidDate))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            // Calculated properties
            .ForMember(dest => dest.OutstandingAmount, opt => opt.MapFrom(src => src.OutstandingAmount))
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => src.IsOverdue))
            .ForMember(dest => dest.DaysOverdue, opt => opt.MapFrom(src => src.DaysOverdue))
            // Navigation properties
            .ForMember(dest => dest.StudentName, opt => opt.MapFrom(src => src.Student != null ? $"{src.Student.FirstName} {src.Student.LastName}" : null))
            .ForMember(dest => dest.Payments, opt => opt.MapFrom(src => src.Payments));

        // CreateFeeReq to Entity mapping
        CreateMap<CreateFeeReq, Fee>()
            .ForMember(dest => dest.FeeId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.StudentId, opt => opt.MapFrom(src => src.StudentId))
            .ForMember(dest => dest.FeeType, opt => opt.MapFrom(src => src.FeeType))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.DueDate, DateTimeKind.Utc)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.PaidAmount, opt => opt.MapFrom(src => 0m))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enums.FeeStatus.Pending))
            .ForMember(dest => dest.IsPaid, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.PaidDate, opt => opt.MapFrom(src => (DateTime?)null))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore());

        // UpdateFeeReq to Entity mapping (for updating existing entities)
        CreateMap<UpdateFeeReq, Fee>()
            .ForMember(dest => dest.FeeType, opt => opt.MapFrom(src => src.FeeType))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.DueDate, DateTimeKind.Utc)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.FeeId, opt => opt.Ignore())
            .ForMember(dest => dest.StudentId, opt => opt.Ignore())
            .ForMember(dest => dest.PaidAmount, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.IsPaid, opt => opt.Ignore())
            .ForMember(dest => dest.PaidDate, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Student, opt => opt.Ignore())
            .ForMember(dest => dest.Payments, opt => opt.Ignore());
    }
}