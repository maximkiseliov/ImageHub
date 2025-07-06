using ImageHub.Application.Abstractions;
using ImageHub.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services) => services.AddServices();

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IImageService, ImageService>();

        return services;
    }
}