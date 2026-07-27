using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IDbConnectionFactory _dbFactory;

        public UserRepository(IDbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<int> CreateAsync(User user)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = @"INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, IsActive, IsDeleted, CreatedAt)
                        VALUES (@FullName, @Email, @PasswordHash, @PhoneNumber, @IsActive, 0, NOW()); SELECT LAST_INSERT_ID();";
            return await conn.ExecuteScalarAsync<int>(sql, user);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Email = @email AND IsDeleted = 0 LIMIT 1", new { email });
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            using var conn = _dbFactory.CreateConnection();
            return await conn.QueryFirstOrDefaultAsync<User>("SELECT * FROM Users WHERE Id = @id AND IsDeleted = 0 LIMIT 1", new { id });
        }

        public async Task<IEnumerable<User>> ListAsync(int page, int pageSize)
        {
            using var conn = _dbFactory.CreateConnection();
            var offset = (page - 1) * pageSize;
            return await conn.QueryAsync<User>("SELECT * FROM Users WHERE IsDeleted = 0 ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset", new { limit = pageSize, offset });
        }

        public async Task SoftDeleteAsync(int id)
        {
            using var conn = _dbFactory.CreateConnection();
            await conn.ExecuteAsync("UPDATE Users SET IsDeleted = 1 WHERE Id = @id", new { id });
        }

        public async Task UpdateAsync(User user)
        {
            using var conn = _dbFactory.CreateConnection();
            var sql = "UPDATE Users SET FullName = @FullName, PhoneNumber = @PhoneNumber, IsActive = @IsActive, UpdatedAt = NOW() WHERE Id = @Id";
            await conn.ExecuteAsync(sql, user);
        }
    }
}
