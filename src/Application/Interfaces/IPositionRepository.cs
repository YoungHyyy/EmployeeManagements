using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IPositionRepository
{
    Task<int> CreateAsync(Position position);
    Task UpdateAsync(Position position);
    Task SoftDeleteAsync(int id);
    Task<Position?> GetByIdAsync(int id);
    Task<IEnumerable<Position>> ListAsync(int page, int pageSize);
}
