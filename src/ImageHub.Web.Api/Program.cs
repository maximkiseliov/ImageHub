using ImageHub.Application;
using ImageHub.Infrastructure;
using ImageHub.Web.Api;
using ImageHub.Web.Api.Constants;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

//TODO: Update
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

builder.Services
    .AddPresentation()
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opt =>
    {
        opt
            .WithTitle(WebApiConstants.Scalar.Title)
            .WithTheme(ScalarTheme.Moon)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .WithDarkMode();
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();