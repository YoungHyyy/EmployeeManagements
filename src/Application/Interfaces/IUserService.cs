using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserResponse?> GetByIdAsync(int id);
        Task<IEnumerable<UserResponse>> ListAsync(int page, int pageSize);
        Task<UserResponse> CreateAsync(CreateUserRequest request);
        Task UpdateAsync(int id, UpdateUserRequest request);
        Task DeleteAsync(int id, int? currentUserId = null);
    }
}
