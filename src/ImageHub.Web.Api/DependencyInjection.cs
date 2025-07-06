using FluentValidation;
using ImageHub.Web.Api.Constants;

namespace ImageHub.Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers();

        services.AddValidatorsFromAssemblyContaining<IWebApiMarker>();

        services.AddOpenApi(WebApiConstants.Scalar.DocumentName);

        return services;
    }
}