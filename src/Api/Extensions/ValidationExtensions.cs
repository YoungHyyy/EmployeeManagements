using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Api.Extensions;

public static class ValidationExtensions
{
    public static IServiceCollection AddAppValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        return services;
    }
}
