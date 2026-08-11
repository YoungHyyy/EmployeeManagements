using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<int> CreateAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task SoftDeleteAsync(int id);
    Task RestoreAsync(int id);
    Task<Employee?> GetByIdAsync(int id);
    Task<(IEnumerable<Employee> Items, int TotalCount)> ListAsync(EmployeeListQuery query);
    Task<bool> ExistsByEmailAsync(string email, int? excludeEmployeeId = null);
}
