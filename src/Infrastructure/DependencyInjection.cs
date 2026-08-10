using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Infrastructure.Data;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Infrastructure (Dapper repositories, DB factory). Controllers must not use these directly.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var defaultConn = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=localhost;Database=EmployeeManagementDb;User Id=emsuser;Password=YourPassword123;";

        services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(defaultConn));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        return services;
    }
}
