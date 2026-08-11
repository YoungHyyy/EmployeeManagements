using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// GenericRepository Create/Get. Đặc thù token: MarkUsed / Revoke (partial update — Dapper).
/// </summary>
public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task AddAsync(RefreshToken token) => CreateAsync(token);

    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        => GetByFieldAsync(nameof(RefreshToken.TokenHash), tokenHash);

    public Task MarkAsUsedAsync(int id, string? replacedByTokenHash = null)
        => UpdatePartialByIdAsync(
            id,
            new { IsUsed = true, ReplacedByToken = replacedByTokenHash },
            requireNotDeleted: false);

    public Task RevokeAsync(string tokenHash)
        => UpdatePartialWhereAsync(
            nameof(RefreshToken.TokenHash),
            tokenHash,
            new { IsRevoked = true, RevokedAt = DateTime.UtcNow },
            requireNotDeleted: false);

    public Task RevokeAllUserTokensAsync(int userId)
        => ExecuteAsync(
            $@"UPDATE `{TableName}`
               SET `IsRevoked` = 1, `RevokedAt` = NOW(), `UpdatedAt` = NOW()
               WHERE `UserId` = @UserId AND `IsRevoked` = 0 AND `IsDeleted` = 0",
            new { UserId = userId });
}
