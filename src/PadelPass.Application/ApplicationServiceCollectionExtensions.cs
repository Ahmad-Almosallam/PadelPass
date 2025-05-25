using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PadelPass.Application.Services.Implementations;

namespace PadelPass.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static void AddApplication(
        this IServiceCollection services)
    {
        // Registers validators from this assembly (FluentValidation)
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);

        // Registers AutoMapper profiles from this assembly
        services.AddAutoMapper(typeof(ApplicationServiceCollectionExtensions).Assembly);
        
        services.AddScoped<ClubService>();
        services.AddScoped<NonPeakSlotService>();
        services.AddScoped<SubscriptionPlanService>();
        services.AddScoped<SubscriptionService>();
        services.AddScoped<AuthService>();
        services.AddScoped<ClubUserService>();
    }
}