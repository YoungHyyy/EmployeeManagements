using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentDto>> GetAllAsync(int page, int pageSize);
    Task<DepartmentDto?> GetByIdAsync(int id);
    Task<DepartmentDto> CreateAsync(DepartmentDto request);
    Task UpdateAsync(int id, DepartmentDto request);
    Task DeleteAsync(int id);
}
