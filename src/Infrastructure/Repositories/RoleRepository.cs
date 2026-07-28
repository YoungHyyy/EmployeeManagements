using Dapper;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;

public class RoleRepository : IRoleRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public RoleRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int?> GetRoleIdByCodeAsync(string roleCode)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int?>("SELECT Id FROM Roles WHERE Code = @Code AND IsDeleted = 0 LIMIT 1", new { Code = roleCode });
    }

    public async Task AssignRoleAsync(int userId, int roleId)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("INSERT IGNORE INTO UserRoles (UserId, RoleId, CreatedAt) VALUES (@UserId, @RoleId, NOW())", new { UserId = userId, RoleId = roleId });
    }
}
