using ImageHub.Application;
using ImageHub.Infrastructure;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, configuration) =>
{
    configuration.ReadFrom.Configuration(context.Configuration);
});

builder.Services.Configure<KestrelServerOptions>(opt =>
{
    opt.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    opt.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});

builder.Services.AddControllers();
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

var app = builder.Build();
app.MapControllers();
app.Run();

// Used in TestWebAppFactory
public partial class Program;