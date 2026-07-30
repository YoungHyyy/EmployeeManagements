namespace EmployeeManagement.Application.Interfaces;

public interface IRoleRepository
{
    Task<int?> GetRoleIdByCodeAsync(string roleCode);
    Task AssignRoleAsync(int userId, int roleId);
    Task<string?> GetRoleCodeByUserIdAsync(int userId);
}
