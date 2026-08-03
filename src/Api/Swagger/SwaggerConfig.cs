using Microsoft.OpenApi.Models;

namespace EmployeeManagement.Api.Swagger;

public static class SwaggerConfig
{
    public static OpenApiInfo GetInfo() => new()
    {
        Title = "Employee Management API",
        Version = "v1",
        Description = "API cho hệ thống quản lý nhân viên bằng .NET 8 + MySQL + Dapper",
        Contact = new OpenApiContact
        {
            Name = "Backend Team",
            Email = "backend@example.com"
        }
    };
}
