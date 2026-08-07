using EmployeeManagement.Api.Configuration;
using EmployeeManagement.Api.Extensions;
using EmployeeManagement.Application.Validators;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day));

builder.Services.AddApiLayer(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddValidationServices();

StartupConfigurationValidator.Validate(builder.Configuration);

var app = builder.Build();
app.UseApiPipeline();

app.Run();
