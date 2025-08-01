using AutoMapper;

using PaymentGateway.Application.DTOs.Responses;
using PaymentGateway.Domain.Entities;

namespace PaymentGateway.Application.Mappers;

public class PaymentMappingProfile : Profile
{
    public PaymentMappingProfile()
    {
        CreateMap<Payment, ProcessPaymentResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CardNumberLastFour, opt => opt.MapFrom(src => src.CardNumberLastFour))
            .ForMember(dest => dest.ExpiryMonth, opt => opt.MapFrom(src => src.ExpiryMonth))
            .ForMember(dest => dest.ExpiryYear, opt => opt.MapFrom(src => src.ExpiryYear))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount));
            
        CreateMap<Payment, GetPaymentByIdResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CardNumberLastFour, opt => opt.MapFrom(src => int.Parse(src.CardNumberLastFour)))
            .ForMember(dest => dest.ExpiryMonth, opt => opt.MapFrom(src => src.ExpiryMonth))
            .ForMember(dest => dest.ExpiryYear, opt => opt.MapFrom(src => src.ExpiryYear))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency.ToString()))
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount));

    }
}