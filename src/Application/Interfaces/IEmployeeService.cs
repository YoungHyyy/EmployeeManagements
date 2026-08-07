using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync(int page, int pageSize, string? search, int? departmentId, int? positionId, string? status);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(EmployeeDto request);
    Task UpdateAsync(int id, EmployeeDto request);
    Task DeleteAsync(int id);
    Task RestoreAsync(int id);
}
