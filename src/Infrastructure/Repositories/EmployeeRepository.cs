using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public EmployeeRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<int> CreateAsync(Employee employee)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            INSERT INTO Employees (UserId, DepartmentId, PositionId, EmployeeCode, FullName, Email, PhoneNumber, DateOfBirth, Gender, Address, HireDate, Status, Salary, ContractStartDate, ContractEndDate, AvatarUrl, IsActive, IsDeleted, CreatedAt)
            VALUES (@UserId, @DepartmentId, @PositionId, @EmployeeCode, @FullName, @Email, @PhoneNumber, @DateOfBirth, @Gender, @Address, @HireDate, @Status, @Salary, @ContractStartDate, @ContractEndDate, @AvatarUrl, @IsActive, 0, NOW());
            SELECT LAST_INSERT_ID();";
        return await conn.ExecuteScalarAsync<int>(sql, employee);
    }

    public async Task UpdateAsync(Employee employee)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"
            UPDATE Employees
            SET DepartmentId = @DepartmentId,
                PositionId = @PositionId,
                FullName = @FullName,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                DateOfBirth = @DateOfBirth,
                Gender = @Gender,
                Address = @Address,
                HireDate = @HireDate,
                Status = @Status,
                Salary = @Salary,
                ContractStartDate = @ContractStartDate,
                ContractEndDate = @ContractEndDate,
                AvatarUrl = @AvatarUrl,
                IsActive = @IsActive,
                UpdatedAt = NOW()
            WHERE Id = @Id";
        await conn.ExecuteAsync(sql, employee);
    }

    public async Task SoftDeleteAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Employees SET IsDeleted = 1, DeletedAt = NOW() WHERE Id = @id", new { id });
    }

    public async Task RestoreAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync("UPDATE Employees SET IsDeleted = 0, DeletedAt = NULL WHERE Id = @id", new { id });
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<Employee>("SELECT * FROM Employees WHERE Id = @id AND IsDeleted = 0 LIMIT 1", new { id });
    }

    public async Task<IEnumerable<Employee>> ListAsync(int page, int pageSize, string? search = null, int? departmentId = null, int? positionId = null, string? status = null)
    {
        using var conn = _dbFactory.CreateConnection();
        var offset = (page - 1) * pageSize;
        var where = "WHERE IsDeleted = 0";
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += " AND (FullName LIKE @search OR Email LIKE @search OR PhoneNumber LIKE @search)";
        }
        if (departmentId.HasValue)
        {
            where += " AND DepartmentId = @departmentId";
        }
        if (positionId.HasValue)
        {
            where += " AND PositionId = @positionId";
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            where += " AND Status = @status";
        }

        var sql = $"SELECT * FROM Employees {where} ORDER BY CreatedAt DESC LIMIT @limit OFFSET @offset";
        return await conn.QueryAsync<Employee>(sql, new { search = $"%{search}%", departmentId, positionId, status, limit = pageSize, offset });
    }
}
