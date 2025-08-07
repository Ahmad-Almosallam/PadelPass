using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PadelPass.Core.Entities;
using PadelPass.Core.Repositories;
using PadelPass.Core.Services;
using PadelPass.Infrastructure.Repositories;
using PadelPass.Infrastructure.Services;

namespace PadelPass.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.AddDbContext<PadelPassDbContext>(opts =>
            opts.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Register service
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<ITimeZoneService, TimeZoneService>();
        services.AddScoped<IGlobalLocalizer, GlobalLocalizer>();

        services.AddIdentityCore<ApplicationUser>(opts => { opts.User.RequireUniqueEmail = true; })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<PadelPassDbContext>()
            .AddTokenProvider(TokenOptions.DefaultProvider, typeof(DataProtectorTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultEmailProvider, typeof(PhoneNumberTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultPhoneProvider, typeof(EmailTokenProvider<ApplicationUser>))
            .AddTokenProvider(TokenOptions.DefaultAuthenticatorProvider,
                typeof(AuthenticatorTokenProvider<ApplicationUser>));


        services.AddScoped(
            typeof(IGenericRepository<>),
            typeof(GenericRepository<>)
        );
    }
}