using Microsoft.Extensions.DependencyInjection;

namespace PadelPass.Core;

public static class CoreServiceCollectionExtensions
{
    public static void AddCore(
        this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Resources");
    }
}