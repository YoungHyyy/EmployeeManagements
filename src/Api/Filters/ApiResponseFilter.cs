using EmployeeManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeManagement.Api.Filters;

public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult)
        {
            context.Result = WrapObjectResult(objectResult);
        }
        else if (context.Result is EmptyResult)
        {
            context.Result = new ObjectResult(new ApiResponse<object>
            {
                Success = true,
                Message = "Thao tác thành công",
                Data = null
            })
            {
                StatusCode = StatusCodes.Status200OK
            };
        }

        await next();
    }

    private static ObjectResult WrapObjectResult(ObjectResult objectResult)
    {
        if (objectResult.Value is ApiResponseBase)
        {
            return objectResult;
        }

        var statusCode = objectResult.StatusCode ?? StatusCodes.Status200OK;
        var isSuccess = statusCode >= 200 && statusCode < 400;

        var response = new ApiResponse<object>
        {
            Success = isSuccess,
            Message = isSuccess ? "Thao tác thành công" : GetFailureMessage(objectResult.Value),
            Data = objectResult.Value
        };

        return new ObjectResult(response)
        {
            StatusCode = statusCode,
            DeclaredType = typeof(ApiResponse<object>)
        };
    }

    private static string GetFailureMessage(object? value)
    {
        return value switch
        {
            ValidationProblemDetails validationProblem => string.Join(" | ", validationProblem.Errors.SelectMany(x => x.Value).Distinct()),
            ProblemDetails problem => string.IsNullOrWhiteSpace(problem.Detail) ? problem.Title ?? "Yêu cầu không hợp lệ" : problem.Detail,
            string message when !string.IsNullOrWhiteSpace(message) => message,
            _ => TryGetMessageProperty(value) ?? "Yêu cầu không hợp lệ"
        };
    }

    private static string? TryGetMessageProperty(object? value)
    {
        if (value is null) return null;

        var prop = value.GetType().GetProperty("message")
                   ?? value.GetType().GetProperty("Message");
        if (prop?.GetValue(value) is string message && !string.IsNullOrWhiteSpace(message))
        {
            return message;
        }

        return null;
    }
}
