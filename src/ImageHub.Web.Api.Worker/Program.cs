using ImageHub.Application;
using ImageHub.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication();

var app = builder.Build();
app.MapControllers();
app.Run();