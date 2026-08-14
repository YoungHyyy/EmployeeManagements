using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>Chỉ kế thừa GenericRepository — CRUD Dapper + Reflection, không EF.</summary>
public class PositionRepository : GenericRepository<Position>, IPositionRepository
{
    public PositionRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task<Position?> GetByCodeAsync(string code)
        => GetByFieldAsync(nameof(Position.Code), code);

    public Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        => ExistsByFieldAsync(nameof(Position.Code), code, excludeId);

    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        => ExistsByFieldAsync(nameof(Position.Name), name, excludeId);
}
