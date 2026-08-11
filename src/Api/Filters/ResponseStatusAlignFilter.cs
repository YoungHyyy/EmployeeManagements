using EmployeeManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeManagement.Api.Filters;

/// <summary>
/// Đồng bộ HTTP status code với body.success (ApiResponse / AuthResponse).
/// Sửa lỗi hay gặp khi test lại cùng 1 API trên Swagger:
/// body đã success=true (có token) nhưng mã HTTP vẫn kẹt 400 (lần fail trước / status lệch).
/// <list type="bullet">
/// <item>success=true  + 4xx/5xx/0 → 200 (giữ 201/2xx nếu đã đúng)</item>
/// <item>success=false + 2xx/0 → 400 (giữ 401/403/404/… nếu đã đúng)</item>
/// </list>
/// </summary>
public sealed class ResponseStatusAlignFilter : IAsyncAlwaysRunResultFilter, IOrderedFilter
{
    /// <summary>Chạy sau ApiResponseFilter (Order cao hơn = sau).</summary>
    public int Order => 10_000;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        Align(context);
        await next();
        AlignHttpResponse(context);
    }

    private static void Align(ResultExecutingContext context)
    {
        if (context.Result is not ObjectResult objectResult || objectResult.Value is null)
        {
            return;
        }

        if (!TryGetSuccess(objectResult.Value, out var success))
        {
            return;
        }

        var current = objectResult.StatusCode
                      ?? (context.HttpContext.Response.StatusCode is > 0
                          ? context.HttpContext.Response.StatusCode
                          : 0);

        if (success)
        {
            // Body thành công mà HTTP vẫn lỗi (vd: 400) → sửa về 200; giữ 201/204/…
            if (current is 0 or >= 400)
            {
                objectResult.StatusCode = StatusCodes.Status200OK;
            }
        }
        else if (current is 0 or (>= 200 and < 300))
        {
            // Body lỗi mà HTTP vẫn 2xx → 400; giữ 401/403/404/…
            objectResult.StatusCode = StatusCodes.Status400BadRequest;
        }

        if (!context.HttpContext.Response.HasStarted && objectResult.StatusCode is int code)
        {
            context.HttpContext.Response.StatusCode = code;
            SetStatusHintHeader(context.HttpContext, code);
        }
    }

    private static void AlignHttpResponse(ResultExecutingContext context)
    {
        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        if (context.Result is not ObjectResult { Value: { } value })
        {
            return;
        }

        if (!TryGetSuccess(value, out var success))
        {
            return;
        }

        var response = context.HttpContext.Response;
        if (success)
        {
            if (response.StatusCode is 0 or >= 400)
            {
                response.StatusCode = StatusCodes.Status200OK;
            }
        }
        else if (response.StatusCode is 0 or (>= 200 and < 300))
        {
            response.StatusCode = StatusCodes.Status400BadRequest;
        }

        if (response.StatusCode > 0)
        {
            SetStatusHintHeader(context.HttpContext, response.StatusCode);
        }
    }

    /// <summary>
    /// Header phụ để xem đúng mã HTTP trong Swagger (Response headers) khi UI "Mã số" bị kẹt.
    /// </summary>
    private static void SetStatusHintHeader(HttpContext http, int statusCode)
    {
        http.Response.Headers["X-Api-Http-Status"] = statusCode.ToString();
    }

    private static bool TryGetSuccess(object value, out bool success)
    {
        success = false;
        if (value is not ApiResponseBase)
        {
            return false;
        }

        var prop = value.GetType().GetProperty("Success");
        if (prop?.GetValue(value) is bool b)
        {
            success = b;
            return true;
        }

        return false;
    }
}
