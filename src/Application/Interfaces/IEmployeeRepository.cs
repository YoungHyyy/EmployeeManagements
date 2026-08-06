using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<int> CreateAsync(Employee employee);
    Task UpdateAsync(Employee employee);
    Task SoftDeleteAsync(int id);
    Task RestoreAsync(int id);
    Task<Employee?> GetByIdAsync(int id);
    Task<IEnumerable<Employee>> ListAsync(int page, int pageSize, string? search = null, int? departmentId = null, int? positionId = null, string? status = null);
    Task<bool> ExistsByEmailAsync(string email, int? excludeEmployeeId = null);
}
