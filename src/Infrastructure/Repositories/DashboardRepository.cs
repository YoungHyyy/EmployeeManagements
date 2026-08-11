using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;


public class DashboardRepository : BaseRepository, IDashboardRepository
{
    public DashboardRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var totals = await QuerySingleAsync<EmployeeTotalsRow>(@"
            SELECT
                COUNT(*) AS TotalEmployees,
                COALESCE(SUM(CASE WHEN Status = 'Working'  THEN 1 ELSE 0 END), 0) AS ActiveEmployees,
                COALESCE(SUM(CASE WHEN Status = 'Resigned' THEN 1 ELSE 0 END), 0) AS ResignedEmployees,
                COALESCE(SUM(CASE
                    WHEN MONTH(CreatedAt) = MONTH(CURRENT_DATE)
                     AND YEAR(CreatedAt)  = YEAR(CURRENT_DATE)
                    THEN 1 ELSE 0 END), 0) AS NewEmployeesThisMonth
            FROM Employees
            WHERE IsDeleted = 0");

        var byDepartment = await QueryListAsync<DepartmentStatDto>(@"
            SELECT d.Name, COUNT(e.Id) AS Count
            FROM Employees e
            LEFT JOIN Departments d ON e.DepartmentId = d.Id
            WHERE e.IsDeleted = 0
            GROUP BY d.Id, d.Name
            ORDER BY Count DESC");

        var byPosition = await QueryListAsync<PositionStatDto>(@"
            SELECT p.Name, COUNT(e.Id) AS Count
            FROM Employees e
            LEFT JOIN Positions p ON e.PositionId = p.Id
            WHERE e.IsDeleted = 0
            GROUP BY p.Id, p.Name
            ORDER BY Count DESC");

        return new DashboardStatsDto
        {
            TotalEmployees = totals?.TotalEmployees ?? 0,
            ActiveEmployees = totals?.ActiveEmployees ?? 0,
            ResignedEmployees = totals?.ResignedEmployees ?? 0,
            NewEmployeesThisMonth = totals?.NewEmployeesThisMonth ?? 0,
            EmployeesByDepartment = byDepartment.ToList(),
            EmployeesByPosition = byPosition.ToList()
        };
    }

    private sealed class EmployeeTotalsRow
    {
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int ResignedEmployees { get; set; }
        public int NewEmployeesThisMonth { get; set; }
    }
}
