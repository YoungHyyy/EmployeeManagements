using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null);
        Task<int> CreateAsync(User user);
        /// <summary>Insert User + UserRoles trong một transaction. Lỗi → rollback cả hai.</summary>
        Task<int> CreateWithRoleAsync(User user, int roleId);
        Task UpdateAsync(User user);
        Task SoftDeleteAsync(int id);
        Task<IEnumerable<User>> ListAsync(int page, int pageSize);
    }
}
