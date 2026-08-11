using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<IEnumerable<EmployeeDto>> GetAllAsync(int page, int pageSize, string? search, int? departmentId, int? positionId, string? status);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(EmployeeDto request, int? actorUserId = null);
    Task UpdateAsync(int id, EmployeeDto request, int? actorUserId = null);
    Task DeleteAsync(int id, int? actorUserId = null);
    Task RestoreAsync(int id, int? actorUserId = null);
}
