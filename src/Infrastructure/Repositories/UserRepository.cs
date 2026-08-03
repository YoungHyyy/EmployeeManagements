using System.Collections.Generic;
using System.Threading.Tasks;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
        {
        }

        public async Task<int> CreateAsync(User user)
        {
            var sql = @"INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, IsActive, IsDeleted, CreatedAt)
                        VALUES (@FullName, @Email, @PasswordHash, @PhoneNumber, @IsActive, 0, NOW()); SELECT LAST_INSERT_ID();";
            return await InsertAsync(sql, user);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await QuerySingleAsync("SELECT * FROM Users WHERE Email = @email AND IsDeleted = 0 LIMIT 1", new { email });
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await QuerySingleAsync("SELECT * FROM Users WHERE Id = @id AND IsDeleted = 0 LIMIT 1", new { id });
        }

        public async Task<IEnumerable<User>> ListAsync(int page, int pageSize)
        {
            var offset = (page - 1) * pageSize;
            return await QueryListAsync("SELECT * FROM Users WHERE IsDeleted = 0 ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset", new { limit = pageSize, offset });
        }

        public async Task SoftDeleteAsync(int id)
        {
            await ExecuteAsync("UPDATE Users SET IsDeleted = 1 WHERE Id = @id", new { id });
        }

        public async Task UpdateAsync(User user)
        {
            var sql = "UPDATE Users SET FullName = @FullName, PhoneNumber = @PhoneNumber, AvatarUrl = @AvatarUrl, IsActive = @IsActive, UpdatedAt = NOW() WHERE Id = @Id";
            await ExecuteAsync(sql, user);
        }
    }
}
