using ImageHub.Application;
using ImageHub.Infrastructure;
using ImageHub.Web.Api;
using ImageHub.Web.Api.Constants;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services
    .AddPresentation()
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

var app = builder.Build();
app.MapOpenApi();
app.MapScalarApiReference(opt =>
{
    opt
        .WithTitle(WebApiConstants.Scalar.Title)
        .WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .WithDarkMode();
});

app.MapControllers();
app.Run();

// Used in TestWebAppFactory
public partial class Program;