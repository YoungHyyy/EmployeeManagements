using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IDepartmentRepository
{
    Task<int> CreateAsync(Department department);
    Task UpdateAsync(Department department);
    Task SoftDeleteAsync(int id);
    Task<Department?> GetByIdAsync(int id);
    Task<IEnumerable<Department>> ListAsync(int page, int pageSize);
}
