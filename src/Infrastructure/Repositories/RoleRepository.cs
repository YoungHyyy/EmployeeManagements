using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// Chức năng đặc thù: Roles/UserRoles JOIN (backend.md §4.3) — SQL tay Dapper, không EF.
/// </summary>
public class RoleRepository : BaseRepository, IRoleRepository
{
    public RoleRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task<int?> GetRoleIdByCodeAsync(string roleCode)
        => ExecuteScalarAsync<int?>(
            @"SELECT Id FROM Roles WHERE Code = @Code AND IsDeleted = 0 LIMIT 1",
            new { Code = roleCode });

    public Task AssignRoleAsync(int userId, int roleId)
        => ExecuteAsync(
            @"INSERT IGNORE INTO UserRoles (UserId, RoleId, CreatedAt)
              VALUES (@UserId, @RoleId, NOW())",
            new { UserId = userId, RoleId = roleId });

    public Task<string?> GetRoleCodeByUserIdAsync(int userId)
        => ExecuteScalarAsync<string?>(
            @"SELECT r.Code
              FROM UserRoles ur
              JOIN Roles r ON ur.RoleId = r.Id
              WHERE ur.UserId = @UserId AND ur.IsDeleted = 0 AND r.IsDeleted = 0
              LIMIT 1",
            new { UserId = userId });
}
