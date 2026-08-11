using System.Text;
using System.Text.Json;
using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Api.Filters;
using EmployeeManagement.Api.Middleware;
using EmployeeManagement.Api.Swagger;
using EmployeeManagement.Application;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

namespace EmployeeManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly JsonSerializerOptions AuthJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IServiceCollection AddApiLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<BindEmployeeIdFromRouteFilter>();
        services.AddControllers(options =>
        {
            // Wrapper body { success, message, data } — giữ 200/201/400/404/…
            options.Filters.Add<ApiResponseFilter>();
            // Đồng bộ HTTP status với body.success (tránh success=true mà vẫn 400 khi test lại API)
            options.Filters.Add<ResponseStatusAlignFilter>();
            options.Filters.AddService<BindEmployeeIdFromRouteFilter>();
        });

        // FluentValidation / ModelState → ApiResponse chuẩn (không bọc ValidationProblemDetails tiếng Anh)
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        k => k.Key,
                        v => v.Value!.Errors
                            .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Giá trị không hợp lệ" : e.ErrorMessage)
                            .ToArray());

                var messages = errors.SelectMany(e => e.Value).Distinct().ToList();
                var message = messages.Count > 0
                    ? string.Join(" | ", messages)
                    : "Dữ liệu không hợp lệ";

                return new BadRequestObjectResult(ApiResponse.FailValidation(message, errors));
            };
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

                options.Events = new JwtBearerEvents
                {
                    // Token sai trên API public (login/register) → bỏ qua, không chặn request
                    OnAuthenticationFailed = context =>
                    {
                        context.NoResult();
                        return Task.CompletedTask;
                    },
                    OnChallenge = async context =>
                    {
                        // Chỉ khi framework thực sự challenge (API có [Authorize])
                        context.HandleResponse();
                        if (context.Response.HasStarted) return;

                        // Đừng ghi đè response đã thành công (login 200).
                        // ContentLength thường null khi Transfer-Encoding: chunked → không dựa vào nó.
                        if (context.Response.StatusCode is >= 200 and < 300
                            && !string.IsNullOrEmpty(context.Response.ContentType))
                        {
                            return;
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var body = ApiResponse.Fail("Chưa đăng nhập hoặc token không hợp lệ");
                        await context.Response.WriteAsync(JsonSerializer.Serialize(body, AuthJsonOptions));
                    },
                    OnForbidden = async context =>
                    {
                        if (context.Response.HasStarted) return;

                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        context.Response.ContentType = "application/json; charset=utf-8";
                        var body = ApiResponse.Fail("Bạn không có quyền thực hiện thao tác này");
                        await context.Response.WriteAsync(JsonSerializer.Serialize(body, AuthJsonOptions));
                    }
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
                // Chặn Google Dịch / auto-translate — dịch trang làm Swagger kẹt mã HTTP (body đổi, mã số không)
                c.HeadContent = """
                    <meta name="google" content="notranslate" />
                    <meta name="googlebot" content="notranslate" />
                    <style>html, body, #swagger-ui { translate: no; }</style>
                    <script>
                      document.documentElement.setAttribute('translate', 'no');
                      document.documentElement.classList.add('notranslate');
                    </script>
                    """;
                // Log status thật ra console (F12) để đối chiếu khi UI hiển thị sai
                c.UseResponseInterceptor(
                    "function (res) { try { console.log('[API]', res.url, '→ HTTP', res.status); } catch (e) {} return res; }");
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
