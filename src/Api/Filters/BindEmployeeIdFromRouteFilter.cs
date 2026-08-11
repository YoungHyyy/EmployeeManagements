using EmployeeManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeManagement.Api.Filters;

/// <summary>
/// Gán route {id} vào EmployeeDto.Id trước FluentValidation,
/// để check email unique exclude đúng bản ghi khi Update.
/// </summary>
public sealed class BindEmployeeIdFromRouteFilter : IActionFilter, IOrderedFilter
{
    // Chạy trước FluentValidation auto-validation filter
    public int Order => -10000;

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ActionArguments.TryGetValue("id", out var idObj) || idObj is not int id || id <= 0)
        {
            return;
        }

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is EmployeeDto dto)
            {
                dto.Id = id;
                break;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
