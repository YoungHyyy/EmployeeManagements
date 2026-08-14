using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IPositionRepository
{
    Task<int> CreateAsync(Position position);
    Task UpdateAsync(Position position);
    Task SoftDeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<Position?> GetByIdAsync(int id);
    Task<Position?> GetByCodeAsync(string code);
    Task<IEnumerable<Position>> ListAsync(int page, int pageSize);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);
    Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
}
