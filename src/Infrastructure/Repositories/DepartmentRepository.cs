using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public DepartmentRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> CreateAsync(Department department)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"INSERT INTO Departments (Code, Name, Description, IsActive, IsDeleted, CreatedAt)
                    VALUES (@Code, @Name, @Description, @IsActive, 0, NOW());
                    SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, department);
    }

    public async Task UpdateAsync(Department department)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"UPDATE Departments SET Code = @Code, Name = @Name, Description = @Description, IsActive = @IsActive, UpdatedAt = NOW()
                    WHERE Id = @Id";
        await conn.ExecuteAsync(sql, department);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Departments SET IsDeleted = 1, DeletedAt = NOW() WHERE Id = @id", new { id });
    }

    public async Task<Department?> GetByIdAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Department>("SELECT * FROM Departments WHERE Id = @id AND IsDeleted = 0 LIMIT 1", new { id });
    }

    public async Task<IEnumerable<Department>> ListAsync(int page, int pageSize)
    {
        using var conn = _dbFactory.CreateConnection();
        var offset = (page - 1) * pageSize;
        return await conn.QueryAsync<Department>("SELECT * FROM Departments WHERE IsDeleted = 0 ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset", new { limit = pageSize, offset });
    }
}
