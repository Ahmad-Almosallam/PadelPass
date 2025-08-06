using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PadelPass.Core.Common.Enums;
using PadelPass.Core.Constants;
using PadelPass.Core.Entities;

namespace PadelPass.Infrastructure.Identity;

public static class DbInitializerExtensions
{
    public static void SeedDatabase(
        this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<IHost>>();

        try
        {
            var dbContext = services.GetRequiredService<PadelPassDbContext>();
            dbContext.Database.Migrate();

            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            SeedRolesAndUsers(roleManager, userManager).GetAwaiter().GetResult();
            
            logger.LogInformation("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database");
        }
    }

    private static async Task SeedRolesAndUsers(
        RoleManager<IdentityRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        // Create roles if they don't exist
        foreach (var role in AppRoles.AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Create super admin if it doesn't exist
        var superAdminEmail = "support@padelpass.com";
        var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

        if (superAdmin == null)
        {
            superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                PhoneNumber = "123456789",
                FullName = "Super Admin",
                EmailConfirmed = true,
                UserType = UserType.Admin,
                SecurityStamp = Guid.NewGuid().ToString()
            };

            var result = await userManager.CreateAsync(superAdmin, "SuperAdmin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, AppRoles.SuperAdmin);
            }
        }
    }
}