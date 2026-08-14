using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// Employee repository — <b>toàn bộ CRUD</b> từ <see cref="GenericRepository{TEntity}"/>
/// (Create / Update / SoftDelete / Restore / GetById — Reflection + Dapper, không EF).
/// <para>
/// Chỉ viết thêm chức năng đặc thù backend.md §5: List search/filter/sort + ExistsByEmail.
/// </para>
/// </summary>
/// <remarks>
/// CRUD kế thừa (không override):
/// <list type="bullet">
/// <item><see cref="GenericRepository{TEntity}.CreateAsync"/></item>
/// <item><see cref="GenericRepository{TEntity}.UpdateAsync"/></item>
/// <item><see cref="GenericRepository{TEntity}.SoftDeleteAsync"/></item>
/// <item><see cref="GenericRepository{TEntity}.RestoreAsync"/></item>
/// <item><see cref="GenericRepository{TEntity}.GetByIdAsync"/></item>
/// </list>
/// Cột INSERT/UPDATE lấy từ property <see cref="Employee"/> + [DbUpdateIgnore] trên UserId/EmployeeCode.
/// </remarks>
public class EmployeeRepository : GenericRepository<Employee>, IEmployeeRepository
{
    private static readonly IReadOnlyDictionary<string, string> SortMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["fullName"] = "FullName",
            ["createdAt"] = "CreatedAt",
            ["hireDate"] = "HireDate"
        };

    private static readonly string[] SearchColumns =
    {
        nameof(Employee.FullName),
        nameof(Employee.Email),
        nameof(Employee.PhoneNumber)
    };

    public EmployeeRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    // ─── CRUD: CreateAsync / UpdateAsync / SoftDeleteAsync / RestoreAsync / GetByIdAsync
    //     → GenericRepository (không viết lại SQL ở đây)

    /// <summary>
    /// Đặc thù: danh sách có search / filter / sort / pagination (không phải CRUD thuần).
    /// </summary>
    public Task<(IEnumerable<Employee> Items, int TotalCount)> ListAsync(EmployeeListQuery query)
    {
        var filter = CreateFilter()
            .LikeAny(SearchColumns, query.Search)
            .Like(nameof(Employee.FullName), query.SearchName)
            .Like(nameof(Employee.Email), query.SearchEmail)
            .Like(nameof(Employee.PhoneNumber), query.SearchPhone)
            .Equal(nameof(Employee.DepartmentId), query.DepartmentId)
            .Equal(nameof(Employee.PositionId), query.PositionId)
            .Equal(nameof(Employee.Status), query.Status)
            .AddPaging(query.Page, query.PageSize);

        var orderBy = SqlFilterBuilder.ResolveOrderBy(query.SortBy, query.SortDir, SortMap);
        return ListByFilterAsync(filter, orderBy);
    }

    public Task<Employee?> GetByEmailAsync(string email)
        => GetByFieldAsync(nameof(Employee.Email), email);

    public Task<Employee?> GetByUserIdAsync(int userId)
        => GetByFieldAsync(nameof(Employee.UserId), userId);

    /// <summary>
    /// Email unique trên cả bản ghi đã xóa mềm (khớp UNIQUE trên DB).
    /// </summary>
    public async Task<bool> ExistsByEmailAsync(string email, int? excludeEmployeeId = null)
    {
        var filter = CreateFilter(softDeleteOnly: false)
            .EqualIgnoreCase(nameof(Employee.Email), email)
            .NotEqual("Id", excludeEmployeeId);
        return await CountByFilterAsync(filter) > 0;
    }

    public async Task<bool> ExistsByEmployeeCodeAsync(string code)
    {
        var filter = CreateFilter(softDeleteOnly: false)
            .EqualIgnoreCase(nameof(Employee.EmployeeCode), code);
        return await CountByFilterAsync(filter) > 0;
    }

    public async Task<bool> ExistsByDepartmentIdAsync(int departmentId)
    {
        var filter = CreateFilter().Equal(nameof(Employee.DepartmentId), departmentId);
        return await CountByFilterAsync(filter) > 0;
    }

    public async Task<bool> ExistsByPositionIdAsync(int positionId)
    {
        var filter = CreateFilter().Equal(nameof(Employee.PositionId), positionId);
        return await CountByFilterAsync(filter) > 0;
    }

    public Task LinkUserAsync(int employeeId, int userId)
        => ExecuteAsync(
            @"UPDATE Employees
              SET UserId = @userId, UpdatedAt = NOW()
              WHERE Id = @employeeId AND IsDeleted = 0",
            new { employeeId, userId });
}
