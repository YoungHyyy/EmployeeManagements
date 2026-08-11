using System.Net;
using System.Text.Json;
using EmployeeManagement.Application.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EmployeeManagement.Api.Middleware;

public class ExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

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

            // Một số pipeline trả 401/403 không body (auth challenge mặc định)
            if (!context.Response.HasStarted
                && context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden
                && (context.Response.ContentLength is null or 0)
                && string.IsNullOrEmpty(context.Response.ContentType))
            {
                await WriteErrorAsync(
                    context,
                    context.Response.StatusCode,
                    context.Response.StatusCode == StatusCodes.Status401Unauthorized
                        ? "Chưa đăng nhập hoặc token không hợp lệ"
                        : "Bạn không có quyền thực hiện thao tác này");
            }
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Exception after response started for {Method} {Path}", context.Request.Method, context.Request.Path);
                throw;
            }

            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            var (statusCode, message) = MapException(ex);
            await WriteErrorAsync(context, (int)statusCode, message);
        }
    }

    private static async Task WriteErrorAsync(HttpContext context, int statusCode, string message)
    {
        context.Response.Clear();
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = statusCode;

        var response = ApiResponse.Fail(message);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }

    private static (HttpStatusCode StatusCode, string Message) MapException(Exception ex)
    {
        return ex switch
        {
            ArgumentException => (
                HttpStatusCode.BadRequest,
                string.IsNullOrWhiteSpace(ex.Message) ? "Yêu cầu không hợp lệ" : ex.Message),

            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                string.IsNullOrWhiteSpace(ex.Message) ? "Yêu cầu không hợp lệ" : ex.Message),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                string.IsNullOrWhiteSpace(ex.Message) ? "Không tìm thấy dữ liệu" : ex.Message),

            FileNotFoundException => (
                HttpStatusCode.NotFound,
                string.IsNullOrWhiteSpace(ex.Message) ? "Không tìm thấy dữ liệu" : ex.Message),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                string.IsNullOrWhiteSpace(ex.Message) ? "Chưa đăng nhập hoặc token không hợp lệ" : ex.Message),

            _ when Contains(ex.Message, "not found") || Contains(ex.Message, "không tìm thấy")
                => (HttpStatusCode.NotFound, string.IsNullOrWhiteSpace(ex.Message) ? "Không tìm thấy dữ liệu" : ex.Message),

            _ when Contains(ex.Message, "already exists") || Contains(ex.Message, "đã tồn tại")
                => (HttpStatusCode.BadRequest, string.IsNullOrWhiteSpace(ex.Message) ? "Dữ liệu đã tồn tại" : ex.Message),

            _ => (HttpStatusCode.InternalServerError, "Đã xảy ra lỗi hệ thống")
        };
    }

    private static bool Contains(string? source, string value)
        => !string.IsNullOrEmpty(source)
           && source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
