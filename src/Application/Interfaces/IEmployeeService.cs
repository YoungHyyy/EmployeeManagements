using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeListResult> GetAllAsync(EmployeeListQuery query);
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto> CreateAsync(EmployeeDto request, int? actorUserId = null);
    Task UpdateAsync(int id, EmployeeDto request, int? actorUserId = null);
    Task DeleteAsync(int id, int? actorUserId = null);
    Task RestoreAsync(int id, int? actorUserId = null);
}
