using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;

public class RoleRepository : BaseRepository<object>, IRoleRepository
{
    public RoleRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public async Task<int?> GetRoleIdByCodeAsync(string roleCode)
    {
        return await QueryScalarAsync<int?>("SELECT Id FROM Roles WHERE Code = @Code AND IsDeleted = 0 LIMIT 1", new { Code = roleCode });
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        await ExecuteAsync("INSERT IGNORE INTO UserRoles (UserId, RoleId, CreatedAt) VALUES (@UserId, @RoleId, NOW())", new { UserId = userId, RoleId = roleId });
    }

    public async Task<string?> GetRoleCodeByUserIdAsync(int userId)
    {
        return await QueryScalarAsync<string?>(@"SELECT r.Code FROM UserRoles ur
            JOIN Roles r ON ur.RoleId = r.Id
            WHERE ur.UserId = @UserId AND ur.IsDeleted = 0 AND r.IsDeleted = 0 LIMIT 1", new { UserId = userId });
    }
}
