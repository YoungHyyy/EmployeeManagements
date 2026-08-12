using System.Net;
using System.Text.Json;
using EmployeeManagement.Application.Common;
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
                        ? ApiMessages.Unauthorized
                        : ApiMessages.Forbidden);
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
        // MySQL unique constraint (duplicate Code/Name/Email...) — không lộ stack SQL ra client
        if (IsDuplicateKey(ex))
        {
            return (HttpStatusCode.BadRequest, MapDuplicateMessage(ex.Message));
        }

        return ex switch
        {
            ArgumentException => (
                HttpStatusCode.BadRequest,
                string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.InvalidRequest : ex.Message),

            InvalidOperationException => (
                HttpStatusCode.BadRequest,
                string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.InvalidRequest : ex.Message),

            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.NotFound : ex.Message),

            FileNotFoundException => (
                HttpStatusCode.NotFound,
                string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.NotFound : ex.Message),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.Unauthorized : ex.Message),

            _ when Contains(ex.Message, "not found") || Contains(ex.Message, "không tìm thấy")
                => (HttpStatusCode.NotFound, string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.NotFound : ex.Message),

            _ when Contains(ex.Message, "already exists") || Contains(ex.Message, "đã tồn tại")
                => (HttpStatusCode.BadRequest, string.IsNullOrWhiteSpace(ex.Message) ? ApiMessages.Conflict : ex.Message),

            // Lỗi DB / chưa map → 500 generic (chi tiết chỉ ghi Serilog)
            _ => (HttpStatusCode.InternalServerError, ApiMessages.SystemError)
        };
    }

    private static bool IsDuplicateKey(Exception ex)
    {
        // MySqlConnector: ErrorCode / Number 1062 = ER_DUP_ENTRY
        for (var e = ex; e != null; e = e.InnerException!)
        {
            var typeName = e.GetType().FullName ?? e.GetType().Name;
            if (typeName.Contains("MySqlException", StringComparison.OrdinalIgnoreCase)
                && (Contains(e.Message, "Duplicate entry") || Contains(e.Message, "duplicate")))
            {
                return true;
            }

            // Reflection: property Number == 1062
            var numberProp = e.GetType().GetProperty("Number") ?? e.GetType().GetProperty("ErrorCode");
            if (numberProp?.GetValue(e) is int n && n == 1062)
            {
                return true;
            }

            if (numberProp?.GetValue(e)?.ToString() == "1062"
                || numberProp?.GetValue(e)?.ToString() == "DuplicateKeyEntry")
            {
                return true;
            }
        }

        return Contains(ex.Message, "Duplicate entry");
    }

    private static string MapDuplicateMessage(string? sqlMessage)
    {
        if (Contains(sqlMessage, "UQ_Departments_Code") || Contains(sqlMessage, "Departments.UQ_Departments_Code"))
            return "Mã phòng ban đã tồn tại";
        if (Contains(sqlMessage, "UQ_Departments_Name"))
            return "Tên phòng ban đã tồn tại";
        if (Contains(sqlMessage, "UQ_Positions_Code"))
            return "Mã chức vụ đã tồn tại";
        if (Contains(sqlMessage, "UQ_Positions_Name"))
            return "Tên chức vụ đã tồn tại";
        if (Contains(sqlMessage, "UQ_Employees_Email") || Contains(sqlMessage, "UQ_Users_Email"))
            return ApiMessages.EmailExists;
        if (Contains(sqlMessage, "EmployeeCode"))
            return "Mã nhân viên đã tồn tại";

        return ApiMessages.Conflict;
    }

    private static bool Contains(string? source, string value)
        => !string.IsNullOrEmpty(source)
           && source.Contains(value, StringComparison.OrdinalIgnoreCase);
}
