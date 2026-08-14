namespace EmployeeManagement.Application.DTOs
{
    /// <summary>
    /// Auth theo chuẩn backend.md §12: { success, message, data }.
    /// Token nằm trong <see cref="ApiResponse{T}.Data"/>.
    /// </summary>
    public class AuthResponse : ApiResponse<AuthTokenDto>
    {
    }
}
