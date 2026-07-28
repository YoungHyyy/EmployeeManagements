using Dapper;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public DashboardRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        using var conn = _dbFactory.CreateConnection();

        var totalEmployees = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Employees WHERE IsDeleted = 0");
        var activeEmployees = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Employees WHERE IsDeleted = 0 AND Status = 'Working'");
        var resignedEmployees = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Employees WHERE IsDeleted = 0 AND Status = 'Resigned'");
        var newEmployeesThisMonth = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Employees WHERE IsDeleted = 0 AND MONTH(CreatedAt) = MONTH(CURRENT_DATE) AND YEAR(CreatedAt) = YEAR(CURRENT_DATE)");

        var byDepartment = await conn.QueryAsync<DepartmentStatDto>(@"
            SELECT d.Name, COUNT(e.Id) AS Count
            FROM Employees e
            LEFT JOIN Departments d ON e.DepartmentId = d.Id
            WHERE e.IsDeleted = 0
            GROUP BY d.Id, d.Name
            ORDER BY Count DESC");

        var byPosition = await conn.QueryAsync<PositionStatDto>(@"
            SELECT p.Name, COUNT(e.Id) AS Count
            FROM Employees e
            LEFT JOIN Positions p ON e.PositionId = p.Id
            WHERE e.IsDeleted = 0
            GROUP BY p.Id, p.Name
            ORDER BY Count DESC");

        return new DashboardStatsDto
        {
            TotalEmployees = totalEmployees,
            ActiveEmployees = activeEmployees,
            ResignedEmployees = resignedEmployees,
            NewEmployeesThisMonth = newEmployeesThisMonth,
            EmployeesByDepartment = byDepartment.ToList(),
            EmployeesByPosition = byPosition.ToList()
        };
    }
}
