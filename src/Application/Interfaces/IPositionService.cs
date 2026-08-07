using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IPositionService
{
    Task<IEnumerable<PositionDto>> GetAllAsync(int page, int pageSize);
    Task<PositionDto?> GetByIdAsync(int id);
    Task<PositionDto> CreateAsync(PositionDto request);
    Task UpdateAsync(int id, PositionDto request);
    Task DeleteAsync(int id);
}
