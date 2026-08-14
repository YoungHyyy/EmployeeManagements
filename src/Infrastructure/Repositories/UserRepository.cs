using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>GenericRepository + query đặc thù GetByEmail / tạo user+role atomic (Dapper).</summary>
public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task<User?> GetByEmailAsync(string email)
        => GetByFieldAsync(nameof(User.Email), email);

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeUserId = null)
    {
        var filter = CreateFilter(softDeleteOnly: false)
            .EqualIgnoreCase(nameof(User.Email), email)
            .NotEqual("Id", excludeUserId);
        return await CountByFilterAsync(filter) > 0;
    }

    public Task<int> CreateWithRoleAsync(User user, int roleId)
    {
        EnsureRoleId(roleId);
        return WithTransactionAsync((conn, tx) => InsertUserWithRoleAsync(conn, tx, user, roleId));
    }

    public Task<(int UserId, int EmployeeId)> CreateWithRoleAndEmployeeAsync(User user, int roleId, Employee employee)
    {
        EnsureRoleId(roleId);
        ArgumentNullException.ThrowIfNull(employee);

        return WithTransactionAsync(async (conn, tx) =>
        {
            var userId = await InsertUserWithRoleAsync(conn, tx, user, roleId);
            employee.UserId = userId;
            var employeeId = await conn.ExecuteScalarAsync<int>(BuildEmployeeInsertSql(), employee, tx);
            employee.Id = employeeId;
            return (userId, employeeId);
        });
    }

    private async Task<int> InsertUserWithRoleAsync(
        System.Data.IDbConnection conn,
        System.Data.IDbTransaction tx,
        User user,
        int roleId)
    {
        var userId = await CreateAsync(user, conn, tx);
        await conn.ExecuteAsync(
            @"INSERT INTO UserRoles (UserId, RoleId, IsDeleted, CreatedAt)
              VALUES (@UserId, @RoleId, 0, NOW())",
            new { UserId = userId, RoleId = roleId },
            tx);
        return userId;
    }

    private static void EnsureRoleId(int roleId)
    {
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "RoleId không hợp lệ.");
        }
    }

    private static string BuildEmployeeInsertSql()
    {
        var columns = EntityColumnMapper
            .GetMap(typeof(Employee), EntityColumnMapper.SoftDeleteSystemColumns)
            .InsertColumns;
        if (columns.Count == 0)
        {
            throw new InvalidOperationException("Employee: không có cột INSERT.");
        }

        EntityColumnMapper.EnsureSafeIdentifiers(columns);
        var cols = string.Join(", ", columns.Select(c => $"`{c}`"));
        var vals = string.Join(", ", columns.Select(c => "@" + c));
        return $@"
            INSERT INTO `Employees` ({cols}, `IsDeleted`, `CreatedAt`)
            VALUES ({vals}, 0, NOW());
            SELECT LAST_INSERT_ID();";
    }
}
