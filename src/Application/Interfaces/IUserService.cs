using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse?> GetByIdAsync(int id);
        Task<IEnumerable<UserResponse>> ListAsync(int page, int pageSize);
        Task<UserResponse> CreateAsync(CreateUserRequest request, int? actorUserId = null);
        Task UpdateAsync(int id, UpdateUserRequest request, int? actorUserId = null);
        Task DeleteAsync(int id, int? currentUserId = null);
    }
}
