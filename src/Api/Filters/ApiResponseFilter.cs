using EmployeeManagement.Application.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeManagement.Api.Filters;

/// <summary>
/// Global Response Wrapper:
/// - Chỉ bọc body về { success, message, data }
/// - <b>Giữ nguyên HTTP status code</b> mà Controller đã thiết lập (200/201/400/401/403/404/...)
/// - Không ép mọi response thành 200
/// </summary>
public class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        context.Result = Normalize(context.Result);
        await next();
    }

    private static IActionResult Normalize(IActionResult? result)
    {
        return result switch
        {
            ObjectResult objectResult => WrapObjectResult(objectResult),

            // Controller return NoContent() → giữ 204 (không đổi thành 200)
            NoContentResult => new ObjectResult(ApiResponse.Ok(message: "Thao tác thành công"))
            {
                StatusCode = StatusCodes.Status204NoContent
            },

            EmptyResult => new ObjectResult(ApiResponse.Ok(message: "Thao tác thành công"))
            {
                StatusCode = StatusCodes.Status200OK
            },

            // Forbid(), Unauthorized() không body → thêm JSON, giữ đúng mã
            StatusCodeResult status when status.StatusCode >= 400 =>
                new ObjectResult(ApiResponse.Fail(DefaultMessageForStatus(status.StatusCode)))
                {
                    StatusCode = status.StatusCode
                },

            _ => result ?? new EmptyResult()
        };
    }

    private static ObjectResult WrapObjectResult(ObjectResult objectResult)
    {
        // Lấy đúng status Controller đã set (Ok=200, Created=201, BadRequest=400, NotFound=404, ...)
        var statusCode = ResolveStatusCode(objectResult);

        // Đã là ApiResponse / AuthResponse → không bọc lại; đồng bộ status với Success
        if (objectResult.Value is ApiResponseBase apiBase)
        {
            statusCode = AlignStatusWithSuccess(statusCode, ReadSuccess(apiBase));
            // Mutate in-place: giữ CreatedAtActionResult (Location), BadRequestObjectResult, …
            objectResult.StatusCode = statusCode;
            return objectResult;
        }

        var isSuccess = statusCode is >= 200 and < 400;

        // Shape { success, message, data? } từ anonymous object
        if (TryReadEnvelope(objectResult.Value, out var existingSuccess, out var existingMessage, out var existingData))
        {
            var bodySuccess = isSuccess && existingSuccess;
            statusCode = AlignStatusWithSuccess(statusCode, bodySuccess);
            objectResult.Value = new ApiResponse<object?>
            {
                Success = bodySuccess,
                Message = string.IsNullOrWhiteSpace(existingMessage)
                    ? (bodySuccess ? "Thao tác thành công" : DefaultMessageForStatus(statusCode))
                    : existingMessage!,
                // Lỗi: không nhét lại envelope vào data
                Data = bodySuccess ? (existingData ?? objectResult.Value) : existingData
            };
            objectResult.StatusCode = statusCode;
            objectResult.DeclaredType = typeof(ApiResponse<object?>);
            return objectResult;
        }

        if (isSuccess)
        {
            objectResult.Value = ApiResponse.Ok(objectResult.Value);
            objectResult.StatusCode = statusCode;
            objectResult.DeclaredType = typeof(ApiResponse<object?>);
            return objectResult;
        }

        var (message, data) = ExtractFailure(objectResult.Value, statusCode);
        objectResult.Value = ApiResponse.Fail(message, data);
        objectResult.StatusCode = statusCode;
        objectResult.DeclaredType = typeof(ApiResponse<object?>);
        return objectResult;
    }

    /// <summary>
    /// Ưu tiên StatusCode trên ObjectResult; nếu null thì suy ra từ kiểu result / body.success.
    /// Không bao giờ “đoán” 200 khi Controller đã trả BadRequest/NotFound/...
    /// </summary>
    private static int ResolveStatusCode(ObjectResult objectResult)
    {
        if (objectResult.StatusCode is int explicitCode)
        {
            return explicitCode;
        }

        return objectResult switch
        {
            BadRequestObjectResult => StatusCodes.Status400BadRequest,
            NotFoundObjectResult => StatusCodes.Status404NotFound,
            UnauthorizedObjectResult => StatusCodes.Status401Unauthorized,
            UnprocessableEntityObjectResult => StatusCodes.Status422UnprocessableEntity,
            ConflictObjectResult => StatusCodes.Status409Conflict,
            CreatedResult => StatusCodes.Status201Created,
            CreatedAtActionResult => StatusCodes.Status201Created,
            CreatedAtRouteResult => StatusCodes.Status201Created,
            AcceptedResult => StatusCodes.Status202Accepted,
            AcceptedAtActionResult => StatusCodes.Status202Accepted,
            AcceptedAtRouteResult => StatusCodes.Status202Accepted,
            OkObjectResult => StatusCodes.Status200OK,
            // ObjectResult thuần + ApiResponse: suy từ Success (Fail → 400, Ok → 200)
            _ when objectResult.Value is ApiResponseBase apiBase
                => ReadSuccess(apiBase) ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status200OK
        };
    }

    /// <summary>
    /// Đồng bộ mã HTTP với body.success:
    /// success=true + 4xx/0 → 200; success=false + 2xx/0 → 400; giữ 201/404/401/…
    /// </summary>
    private static int AlignStatusWithSuccess(int statusCode, bool success)
    {
        if (success)
        {
            return statusCode is 0 or >= 400 ? StatusCodes.Status200OK : statusCode;
        }

        return statusCode is 0 or (>= 200 and < 300)
            ? StatusCodes.Status400BadRequest
            : statusCode;
    }

    private static bool ReadSuccess(ApiResponseBase value)
    {
        var prop = value.GetType().GetProperty("Success");
        return prop?.GetValue(value) is bool b && b;
    }

    private static (string Message, object? Data) ExtractFailure(object? value, int statusCode)
    {
        switch (value)
        {
            case ValidationProblemDetails validation:
            {
                var errors = validation.Errors.ToDictionary(k => k.Key, v => v.Value);
                var messages = errors.SelectMany(e => e.Value).Distinct().ToList();
                var message = messages.Count > 0
                    ? string.Join(" | ", messages)
                    : "Dữ liệu không hợp lệ";
                return (message, errors);
            }
            case ProblemDetails problem:
            {
                var msg = !string.IsNullOrWhiteSpace(problem.Detail)
                    ? problem.Detail!
                    : (!string.IsNullOrWhiteSpace(problem.Title)
                       && problem.Title != "One or more validation errors occurred."
                        ? problem.Title!
                        : DefaultMessageForStatus(statusCode));
                return (msg, null);
            }
            case string s when !string.IsNullOrWhiteSpace(s):
                return (s, null);
            default:
            {
                var fromProp = TryGetStringProperty(value, "message")
                               ?? TryGetStringProperty(value, "Message");
                return (fromProp ?? DefaultMessageForStatus(statusCode), null);
            }
        }
    }

    private static bool TryReadEnvelope(object? value, out bool success, out string? message, out object? data)
    {
        success = false;
        message = null;
        data = null;
        if (value is null) return false;

        var type = value.GetType();
        var successProp = type.GetProperty("success") ?? type.GetProperty("Success");
        var messageProp = type.GetProperty("message") ?? type.GetProperty("Message");
        if (successProp is null || messageProp is null) return false;
        if (successProp.PropertyType != typeof(bool) && successProp.PropertyType != typeof(bool?)) return false;

        success = successProp.GetValue(value) is bool b && b;
        message = messageProp.GetValue(value) as string;
        var dataProp = type.GetProperty("data") ?? type.GetProperty("Data");
        data = dataProp?.GetValue(value);
        return true;
    }

    private static string? TryGetStringProperty(object? value, string name)
    {
        if (value is null) return null;
        return value.GetType().GetProperty(name)?.GetValue(value) as string;
    }

    private static string DefaultMessageForStatus(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Yêu cầu không hợp lệ",
        StatusCodes.Status401Unauthorized => "Chưa đăng nhập hoặc token không hợp lệ",
        StatusCodes.Status403Forbidden => "Bạn không có quyền thực hiện thao tác này",
        StatusCodes.Status404NotFound => "Không tìm thấy dữ liệu",
        StatusCodes.Status409Conflict => "Dữ liệu bị xung đột",
        StatusCodes.Status422UnprocessableEntity => "Dữ liệu không hợp lệ",
        StatusCodes.Status500InternalServerError => "Đã xảy ra lỗi hệ thống",
        _ when statusCode >= 400 && statusCode < 500 => "Yêu cầu không hợp lệ",
        _ => "Đã xảy ra lỗi hệ thống"
    };
}
