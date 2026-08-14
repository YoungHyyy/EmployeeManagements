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
        if (roleId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(roleId), "RoleId không hợp lệ.");
        }

        return WithTransactionAsync(async (conn, tx) =>
        {
            var userId = await CreateAsync(user, conn, tx);
            // INSERT thường (không IGNORE): FK/unique lỗi → ném exception → rollback user
            await conn.ExecuteAsync(
                @"INSERT INTO UserRoles (UserId, RoleId, IsDeleted, CreatedAt)
                  VALUES (@UserId, @RoleId, 0, NOW())",
                new { UserId = userId, RoleId = roleId },
                tx);
            return userId;
        });
    }
}
