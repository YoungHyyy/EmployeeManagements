using System.Net;
using System.Text.Json;
using EmployeeManagement.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.ContentType = "application/json";

            var (statusCode, message) = MapException(ex);
            context.Response.StatusCode = (int)statusCode;

            var response = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

    private static (HttpStatusCode StatusCode, string Message) MapException(Exception ex)
    {
        return ex switch
        {
            ArgumentException or InvalidOperationException
                => (HttpStatusCode.BadRequest, string.IsNullOrWhiteSpace(ex.Message) ? "Yêu cầu không hợp lệ" : ex.Message),
            KeyNotFoundException or FileNotFoundException
                => (HttpStatusCode.NotFound, string.IsNullOrWhiteSpace(ex.Message) ? "Không tìm thấy dữ liệu" : ex.Message),
            UnauthorizedAccessException
                => (HttpStatusCode.Unauthorized, "Không có quyền truy cập"),
            _ when ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.NotFound, "Không tìm thấy dữ liệu"),
            _ when ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.BadRequest, "Dữ liệu đã tồn tại"),
            _ when ex.Message.Contains("không tìm thấy", StringComparison.OrdinalIgnoreCase)
                => (HttpStatusCode.NotFound, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống")
        };
    }
}
