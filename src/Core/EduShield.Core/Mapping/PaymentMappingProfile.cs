using AutoMapper;
using EduShield.Core.Dtos;
using EduShield.Core.Entities;

namespace EduShield.Core.Mapping;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        // Entity to DTO mapping
        CreateMap<Payment, PaymentDto>()
            .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => src.PaymentId))
            .ForMember(dest => dest.FeeId, opt => opt.MapFrom(src => src.FeeId))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => src.PaymentDate))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
            .ForMember(dest => dest.TransactionReference, opt => opt.MapFrom(src => src.TransactionReference))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

        // PaymentReq to Entity mapping
        CreateMap<PaymentReq, Payment>()
            .ForMember(dest => dest.PaymentId, opt => opt.MapFrom(src => Guid.NewGuid()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.PaymentDate, opt => opt.MapFrom(src => DateTime.SpecifyKind(src.PaymentDate, DateTimeKind.Utc)))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod))
            .ForMember(dest => dest.TransactionReference, opt => opt.MapFrom(src => src.TransactionReference))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.FeeId, opt => opt.Ignore()) // This will be set by the service
            .ForMember(dest => dest.Fee, opt => opt.Ignore());
    }
}