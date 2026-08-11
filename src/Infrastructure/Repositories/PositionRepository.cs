using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>Chỉ kế thừa GenericRepository — CRUD Dapper + Reflection, không EF.</summary>
public class PositionRepository : GenericRepository<Position>, IPositionRepository
{
    public PositionRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }
}
