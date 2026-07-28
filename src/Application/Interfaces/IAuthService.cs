using System.Threading.Tasks;
using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RefreshTokenAsync(string refreshToken);
        Task<AuthResponse> LogoutAsync(string refreshToken);
        Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    }
}
