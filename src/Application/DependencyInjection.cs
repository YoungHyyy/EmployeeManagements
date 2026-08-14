using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services (use-cases). No infrastructure dependencies here.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IPositionService, PositionService>();
        services.AddScoped<IEmployeeAccountService, EmployeeAccountService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAvatarUploadService, AvatarUploadService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        return services;
    }
}
