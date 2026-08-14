using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IDepartmentRepository
{
    Task<int> CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task SoftDeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<Department?> GetByIdAsync(int id);
    Task<Department?> GetByCodeAsync(string code);
    Task<IEnumerable<Department>> ListAsync(int page, int pageSize);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
}
