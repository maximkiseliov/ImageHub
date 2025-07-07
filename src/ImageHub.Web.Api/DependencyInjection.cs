using FluentValidation;
using ImageHub.Web.Api.Constants;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace ImageHub.Web.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.Configure<IISServerOptions>(opt =>
            opt.MaxRequestBodySize = WebApiConstants.MaxFileSize);

        services.Configure<KestrelServerOptions>(opt =>
        {
            opt.Limits.MaxRequestBodySize = WebApiConstants.MaxFileSize;
            opt.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
            opt.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
        });

        services.Configure<FormOptions>(opt =>
            opt.MultipartBodyLengthLimit = WebApiConstants.MaxFileSize);
        
        services.AddControllers();

        services.AddValidatorsFromAssemblyContaining<IWebApiMarker>();

        services.AddOpenApi(WebApiConstants.Scalar.DocumentName);

        return services;
    }
}