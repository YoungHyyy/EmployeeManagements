using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IDashboardRepository
{
    Task<DashboardStatsDto> GetStatsAsync();
}
