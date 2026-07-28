namespace EmployeeManagement.Application.DTOs;

public class DashboardStatsDto
{
    public int TotalEmployees { get; set; }
    public int ActiveEmployees { get; set; }
    public int ResignedEmployees { get; set; }
    public int NewEmployeesThisMonth { get; set; }
    public IEnumerable<DepartmentStatDto> EmployeesByDepartment { get; set; } = Array.Empty<DepartmentStatDto>();
    public IEnumerable<PositionStatDto> EmployeesByPosition { get; set; } = Array.Empty<PositionStatDto>();
}

public class DepartmentStatDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PositionStatDto
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}
