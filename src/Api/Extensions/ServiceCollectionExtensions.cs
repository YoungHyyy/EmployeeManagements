using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Api.Filters;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.Swagger;
using EmployeeManagement.Application;
using EmployeeManagement.Infrastructure;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

namespace EmployeeManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<BindEmployeeIdFromRouteFilter>();
        services.AddControllers(options =>
        {
            options.Filters.Add<ApiResponseFilter>();
            // Bind route {id} → EmployeeDto.Id before FluentValidation (email unique exclude)
            options.Filters.AddService<BindEmployeeIdFromRouteFilter>();
        });

        var jwtSettings = configuration.GetSection("Jwt");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured"))),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization(options => AuthorizationPolicies.AddPolicies(options));
        services.AddHealthChecks();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", SwaggerConfig.GetInfo());
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Management API v1");
                c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
                c.DefaultModelsExpandDepth(-1);
            });
        }

        // Only redirect HTTPS when not pure-HTTP local dev (avoids Swagger break on :5269)
        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Serve uploaded avatars under /uploads/*
        var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
        Directory.CreateDirectory(wwwroot);
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwroot),
            RequestPath = ""
        });

        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseSerilogRequestLogging();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();

        return app;
    }

    /// <summary>
    /// Wires Clean Architecture layers: Application (services) + Infrastructure (Dapper/repos).
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services;
    }
}
