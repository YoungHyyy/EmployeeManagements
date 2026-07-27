using System.Threading.Tasks;
using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
