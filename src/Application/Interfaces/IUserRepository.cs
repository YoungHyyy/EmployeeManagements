using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByEmailAsync(string email);
        Task<int> CreateAsync(User user);
        Task UpdateAsync(User user);
        Task SoftDeleteAsync(int id);
        Task<IEnumerable<User>> ListAsync(int page, int pageSize);
    }
}
