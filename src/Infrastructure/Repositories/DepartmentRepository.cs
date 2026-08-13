using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>Chỉ kế thừa GenericRepository — CRUD Dapper + Reflection, không EF.</summary>
public class DepartmentRepository : GenericRepository<Department>, IDepartmentRepository
{
    public DepartmentRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task<bool> ExistsByCodeAsync(string code, int? excludeId = null)
        => ExistsByFieldAsync(nameof(Department.Code), code, excludeId);

    public Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        => ExistsByFieldAsync(nameof(Department.Name), name, excludeId);
}
