namespace EmployeeManagement.Application.DTOs;

/// <summary>
/// Response chuẩn toàn API: { success, message, data }
/// </summary>
public class ApiResponse<T> : ApiResponseBase
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T? data, string message = "Thao tác thành công")
        => new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, T? data = default)
        => new() { Success = false, Message = message, Data = data };
}

/// <summary>Helper không generic cho lỗi / empty data.</summary>
public static class ApiResponse
{
    public static ApiResponse<object?> Ok(object? data = null, string message = "Thao tác thành công")
        => ApiResponse<object?>.Ok(data, message);

    public static ApiResponse<object?> Fail(string message, object? data = null)
        => ApiResponse<object?>.Fail(message, data);

    public static ApiResponse<object?> FailValidation(string message, IDictionary<string, string[]> errors)
        => ApiResponse<object?>.Fail(message, errors);
}
