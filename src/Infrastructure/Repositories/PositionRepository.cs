using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

public class PositionRepository : IPositionRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public PositionRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> CreateAsync(Position position)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"INSERT INTO Positions (Code, Name, Description, Level, IsActive, IsDeleted, CreatedAt)
                    VALUES (@Code, @Name, @Description, @Level, @IsActive, 0, NOW());
                    SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, position);
    }

    public async Task UpdateAsync(Position position)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"UPDATE Positions SET Code = @Code, Name = @Name, Description = @Description, Level = @Level, IsActive = @IsActive, UpdatedAt = NOW()
                    WHERE Id = @Id";
        await conn.ExecuteAsync(sql, position);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Positions SET IsDeleted = 1, DeletedAt = NOW() WHERE Id = @id", new { id });
    }

    public async Task<Position?> GetByIdAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Position>("SELECT * FROM Positions WHERE Id = @id AND IsDeleted = 0 LIMIT 1", new { id });
    }

    public async Task<IEnumerable<Position>> ListAsync(int page, int pageSize)
    {
        using var conn = _dbFactory.CreateConnection();
        var offset = (page - 1) * pageSize;
        return await conn.QueryAsync<Position>("SELECT * FROM Positions WHERE IsDeleted = 0 ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset", new { limit = pageSize, offset });
    }
}
