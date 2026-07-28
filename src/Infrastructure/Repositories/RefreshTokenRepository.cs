using System;
using System.Threading.Tasks;
using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories
{
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public RefreshTokenRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task AddAsync(RefreshToken token)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = @"INSERT INTO RefreshTokens (UserId, TokenHash, JwtId, ExpiresAt, IsUsed, IsRevoked, IsDeleted, CreatedAt)
                        VALUES (@UserId, @TokenHash, @JwtId, @ExpiresAt, @IsUsed, @IsRevoked, @IsDeleted, NOW())";
            await conn.ExecuteAsync(sql, token);
        }

        public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = @"SELECT Id, UserId, TokenHash, JwtId, ExpiresAt, RevokedAt, ReplacedByToken, IsUsed, IsRevoked, IsDeleted, CreatedAt, UpdatedAt, DeletedAt
                        FROM RefreshTokens 
                        WHERE TokenHash = @TokenHash AND IsDeleted = 0 
                        LIMIT 1";
            return await conn.QueryFirstOrDefaultAsync<RefreshToken>(sql, new { TokenHash = tokenHash });
        }

        public async Task MarkAsUsedAsync(int id, string? replacedByTokenHash = null)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = @"UPDATE RefreshTokens 
                        SET IsUsed = 1, ReplacedByToken = @ReplacedByToken, UpdatedAt = NOW() 
                        WHERE Id = @Id";
            await conn.ExecuteAsync(sql, new { Id = id, ReplacedByToken = replacedByTokenHash });
        }

        public async Task RevokeAsync(string tokenHash)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = @"UPDATE RefreshTokens 
                        SET IsRevoked = 1, RevokedAt = NOW(), UpdatedAt = NOW() 
                        WHERE TokenHash = @TokenHash";
            await conn.ExecuteAsync(sql, new { TokenHash = tokenHash });
        }

        public async Task RevokeAllUserTokensAsync(int userId)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = @"UPDATE RefreshTokens 
                        SET IsRevoked = 1, RevokedAt = NOW(), UpdatedAt = NOW() 
                        WHERE UserId = @UserId AND IsRevoked = 0 AND IsDeleted = 0";
            await conn.ExecuteAsync(sql, new { UserId = userId });
        }
    }
}
