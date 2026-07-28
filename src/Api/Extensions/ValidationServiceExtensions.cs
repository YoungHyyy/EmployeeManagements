using EmployeeManagement.Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;

namespace EmployeeManagement.Api.Extensions;

public static class ValidationServiceExtensions
{
    public static IServiceCollection AddValidationServices(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddValidatorsFromAssemblyContaining<EmployeeDtoValidator>();
        return services;
    }
}
