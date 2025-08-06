using AutoMapper;
using PadelPass.Application.DTOs.Clubs;
using PadelPass.Application.DTOs.ClubUsers;
using PadelPass.Application.DTOs.NonPeakSlots;
using PadelPass.Application.DTOs.SubscriptionPlans;
using PadelPass.Application.DTOs.Subscriptions;
using PadelPass.Core.Entities;

namespace PadelPass.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Club mappings
        CreateMap<Club, ClubDto>();
        CreateMap<CreateClubDto, Club>();
        CreateMap<UpdateClubDto, Club>();

        // NonPeakSlot mappings
        CreateMap<NonPeakSlot, NonPeakSlotDto>()
            .ForMember(dest => dest.ClubName, opt => opt.MapFrom(src => src.Club.Name));
        CreateMap<CreateNonPeakSlotDto, NonPeakSlot>();
        CreateMap<UpdateNonPeakSlotDto, NonPeakSlot>();

        // SubscriptionPlan mappings
        CreateMap<SubscriptionPlan, SubscriptionPlanDto>();
        CreateMap<CreateSubscriptionPlanDto, SubscriptionPlan>();
        CreateMap<UpdateSubscriptionPlanDto, SubscriptionPlan>();

        // Subscription mappings
        CreateMap<Subscription, SubscriptionDto>()
            .ForMember(dest => dest.PlanName, opt => opt.MapFrom(src => src.Plan.Name))
            .ForMember(dest => dest.PlanPrice, opt => opt.MapFrom(src => src.Plan.Price))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => string.Empty)); // Will be populated by service
        CreateMap<CreateSubscriptionDto, Subscription>()
            .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow))
            .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => DateTimeOffset.UtcNow.AddMonths(1))) // Default 1 month, will be updated based on plan
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.IsPaused, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.PauseDate, opt => opt.MapFrom(src => (DateTimeOffset?)null))
            .ForMember(dest => dest.RemainingDays, opt => opt.MapFrom(src => (int?)null));
        CreateMap<UpdateSubscriptionDto, Subscription>();
        
        
        
        CreateMap<ClubUser, ClubUserDto>()
            .ForMember(dest => dest.ClubName, opt => opt.MapFrom(src => src.Club.Name))
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User.UserName))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User.Email));
    }
}