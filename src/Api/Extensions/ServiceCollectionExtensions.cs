using EmployeeManagement.Api.Services;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Infrastructure.Data;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;

namespace EmployeeManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultConn = configuration.GetConnectionString("DefaultConnection") ?? "Server=localhost;Database=EmployeeManagementDb;User Id=emsuser;Password=YourPassword123;";

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(defaultConn));
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IAvatarUploadService, AvatarUploadService>();
        return services;
    }
}
