using Dapper;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    private static readonly HashSet<string> AllowedSortColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "fullName", "createdAt", "hireDate"
    };

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
        return await conn.QueryFirstOrDefaultAsync<Employee>(
            "SELECT * FROM Employees WHERE Id = @id AND IsDeleted = 0 LIMIT 1", new { id });
    }

    public async Task<(IEnumerable<Employee> Items, int TotalCount)> ListAsync(EmployeeListQuery query)
    {
        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;
        var offset = (page - 1) * pageSize;

        var where = "WHERE IsDeleted = 0";
        var parameters = new DynamicParameters();
        parameters.Add("limit", pageSize);
        parameters.Add("offset", offset);

        // Combined search: name OR email OR phone
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            where += " AND (FullName LIKE @search OR Email LIKE @search OR PhoneNumber LIKE @search)";
            parameters.Add("search", $"%{query.Search.Trim()}%");
        }

        // Separate field search (AND with other conditions)
        if (!string.IsNullOrWhiteSpace(query.SearchName))
        {
            where += " AND FullName LIKE @searchName";
            parameters.Add("searchName", $"%{query.SearchName.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(query.SearchEmail))
        {
            where += " AND Email LIKE @searchEmail";
            parameters.Add("searchEmail", $"%{query.SearchEmail.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(query.SearchPhone))
        {
            where += " AND PhoneNumber LIKE @searchPhone";
            parameters.Add("searchPhone", $"%{query.SearchPhone.Trim()}%");
        }

        if (query.DepartmentId.HasValue)
        {
            where += " AND DepartmentId = @departmentId";
            parameters.Add("departmentId", query.DepartmentId.Value);
        }

        if (query.PositionId.HasValue)
        {
            where += " AND PositionId = @positionId";
            parameters.Add("positionId", query.PositionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            where += " AND Status = @status";
            parameters.Add("status", query.Status.Trim());
        }

        var orderBy = ResolveOrderBy(query.SortBy, query.SortDir);

        var countSql = $"SELECT COUNT(1) FROM Employees {where}";
        var listSql = $@"
            SELECT * FROM Employees
            {where}
            ORDER BY {orderBy}
            LIMIT @limit OFFSET @offset";

        using var conn = _dbFactory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await conn.QueryAsync<Employee>(listSql, parameters);
        return (items, total);
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeEmployeeId = null)
    {
        using var conn = _dbFactory.CreateConnection();
        var sql = @"SELECT COUNT(1) FROM Employees WHERE LOWER(Email) = LOWER(@email) AND IsDeleted = 0";
        if (excludeEmployeeId.HasValue)
        {
            sql += " AND Id <> @excludeEmployeeId";
        }

        var count = await conn.ExecuteScalarAsync<int>(sql, new { email, excludeEmployeeId });
        return count > 0;
    }

    /// <summary>
    /// Whitelist sort columns to prevent SQL injection.
    /// </summary>
    private static string ResolveOrderBy(string? sortBy, string? sortDir)
    {
        var column = (sortBy ?? "createdAt").Trim();
        if (!AllowedSortColumns.Contains(column))
        {
            column = "createdAt";
        }

        var sqlColumn = column.ToLowerInvariant() switch
        {
            "fullname" => "FullName",
            "hiredate" => "HireDate",
            _ => "CreatedAt"
        };

        var dir = string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";
        return $"{sqlColumn} {dir}, Id {dir}";
    }
}
