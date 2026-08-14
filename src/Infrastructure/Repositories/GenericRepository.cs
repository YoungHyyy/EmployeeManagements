using System.Data;
using System.Reflection;
using Dapper;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Attributes;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// <b>Generic Base Repository</b> — Reflection + Dapper (bắt buộc), <b>không dùng Entity Framework</b>.
/// <list type="bullet">
/// <item>CRUD soft-delete: SQL INSERT/UPDATE/SoftDelete/Get/List sinh từ property entity (cache Reflection)</item>
/// <item>Tên bảng: <see cref="DbTableAttribute"/> trên entity</item>
/// <item>Thêm field: property entity + cột MySQL — không sửa SQL Create/Update ở repo con</item>
/// <item>Repo con chỉ viết query cho chức năng đặc thù (search, JOIN, revoke…)</item>
/// </list>
/// </summary>
public abstract class GenericRepository<TEntity> : BaseRepository<TEntity> where TEntity : class
{
    private readonly string _tableName;

    protected GenericRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
        _tableName = ResolveTableName();
    }

    /// <summary>Tên bảng MySQL từ [DbTable]; override khi cần.</summary>
    protected virtual string TableName => _tableName;

    protected virtual IReadOnlyList<string>? InsertColumnsOverride => null;
    protected virtual IReadOnlyList<string>? UpdateColumnsOverride => null;
    protected virtual string DefaultOrderBy => "`CreatedAt` DESC";

    private EntityColumnMapper.ColumnMap Map =>
        EntityColumnMapper.GetMap(typeof(TEntity), EntityColumnMapper.SoftDeleteSystemColumns);

    private IReadOnlyList<string> InsertColumns => InsertColumnsOverride ?? Map.InsertColumns;
    private IReadOnlyList<string> UpdateColumns => UpdateColumnsOverride ?? Map.UpdateColumns;

    protected IReadOnlyList<string> AllMappedColumns => Map.AllColumns;

    private bool HasColumn(string name) =>
        AllMappedColumns.Any(c => string.Equals(c, name, StringComparison.OrdinalIgnoreCase));

    // ═══════════════════════════════════════════════════════════════════════
    // CRUD tự động (Dapper parameterized — không EF)
    // ═══════════════════════════════════════════════════════════════════════

    public virtual Task<int> CreateAsync(TEntity entity)
        => InsertAsync(BuildInsertSql(), entity);

    protected Task<int> CreateAsync(TEntity entity, IDbConnection conn, IDbTransaction tx)
        => conn.ExecuteScalarAsync<int>(BuildInsertSql(), entity, tx);

    private string BuildInsertSql()
    {
        var columns = InsertColumns;
        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"{typeof(TEntity).Name}: không có cột INSERT.");
        }

        EntityColumnMapper.EnsureSafeIdentifiers(columns);
        var cols = string.Join(", ", columns.Select(c => $"`{c}`"));
        var vals = string.Join(", ", columns.Select(c => "@" + c));

        return $@"
            INSERT INTO `{TableName}` ({cols}, `IsDeleted`, `CreatedAt`)
            VALUES ({vals}, 0, NOW());
            SELECT LAST_INSERT_ID();";
    }

    public virtual async Task UpdateAsync(TEntity entity)
    {
        var columns = UpdateColumns;
        if (columns.Count == 0)
        {
            throw new InvalidOperationException($"{typeof(TEntity).Name}: không có cột UPDATE.");
        }

        EntityColumnMapper.EnsureSafeIdentifiers(columns);
        var set = string.Join(", ", columns.Select(c => $"`{c}` = @{c}"));
        var sql = $@"
            UPDATE `{TableName}`
            SET {set}, `UpdatedAt` = NOW()
            WHERE `Id` = @Id AND `IsDeleted` = 0";

        await ExecuteAsync(sql, entity);
    }

    public virtual Task SoftDeleteAsync(int id)
    {
        var deletedAt = HasColumn("DeletedAt") ? ", `DeletedAt` = NOW()" : string.Empty;
        var sql = $@"
            UPDATE `{TableName}`
            SET `IsDeleted` = 1{deletedAt}, `UpdatedAt` = NOW()
            WHERE `Id` = @id AND `IsDeleted` = 0";
        return ExecuteAsync(sql, new { id });
    }

    public virtual Task RestoreAsync(int id)
    {
        var deletedAt = HasColumn("DeletedAt") ? ", `DeletedAt` = NULL" : string.Empty;
        var sql = $@"
            UPDATE `{TableName}`
            SET `IsDeleted` = 0{deletedAt}, `UpdatedAt` = NOW()
            WHERE `Id` = @id";
        return ExecuteAsync(sql, new { id });
    }

    public virtual Task<TEntity?> GetByIdAsync(int id)
    {
        var sql = $@"
            SELECT * FROM `{TableName}`
            WHERE `Id` = @id AND `IsDeleted` = 0
            LIMIT 1";
        return QuerySingleAsync(sql, new { id });
    }

    public virtual async Task<IEnumerable<TEntity>> ListAsync(int page = 1, int pageSize = 20)
    {
        var filter = CreateFilter().AddPaging(page, pageSize);
        var (items, _) = await ListByFilterAsync(filter, DefaultOrderBy);
        return items;
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        var count = await ExecuteScalarAsync<int>(
            $@"SELECT COUNT(1) FROM `{TableName}` WHERE `Id` = @id AND `IsDeleted` = 0",
            new { id });
        return count > 0;
    }

    public virtual async Task<bool> ExistsIncludingDeletedAsync(int id)
    {
        var count = await ExecuteScalarAsync<int>(
            $@"SELECT COUNT(1) FROM `{TableName}` WHERE `Id` = @id",
            new { id });
        return count > 0;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Helpers cho chức năng đặc thù (repo con dùng — vẫn Dapper)
    // ═══════════════════════════════════════════════════════════════════════

    protected Task<TEntity?> GetByFieldAsync(string column, object value)
    {
        EnsureEntityColumn(column);
        var sql = $@"
            SELECT * FROM `{TableName}`
            WHERE `{column}` = @value AND `IsDeleted` = 0
            LIMIT 1";
        return QuerySingleAsync(sql, new { value });
    }

    protected async Task<bool> ExistsByFieldAsync(string column, object value, int? excludeId = null)
    {
        var filter = CreateFilter()
            .Equal(column, value)
            .NotEqual("Id", excludeId);
        return await CountByFilterAsync(filter) > 0;
    }

    /// <summary>EXISTS theo cột string, so khớp không phân biệt hoa thường (vd: Email).</summary>
    protected async Task<bool> ExistsByFieldIgnoreCaseAsync(string column, string value, int? excludeId = null)
    {
        var filter = CreateFilter()
            .EqualIgnoreCase(column, value)
            .NotEqual("Id", excludeId);
        return await CountByFilterAsync(filter) > 0;
    }

    /// <summary>
    /// UPDATE một phần theo Id — patch object (anonymous / DTO).
    /// Dùng cho chức năng đặc thù (vd: MarkAsUsed), không full UpdateAsync.
    /// </summary>
    protected Task UpdatePartialByIdAsync(int id, object patch, bool requireNotDeleted = true)
    {
        var (setSql, param) = BuildPartialSet(patch);
        param.Add("Id", id);
        var whereDeleted = requireNotDeleted ? " AND `IsDeleted` = 0" : string.Empty;
        var sql = $@"
            UPDATE `{TableName}`
            SET {setSql}, `UpdatedAt` = NOW()
            WHERE `Id` = @Id{whereDeleted}";
        return ExecuteAsync(sql, param);
    }

    /// <summary>
    /// UPDATE một phần theo 1 cột điều kiện (vd: TokenHash).
    /// </summary>
    protected Task UpdatePartialWhereAsync(
        string whereColumn,
        object whereValue,
        object patch,
        bool requireNotDeleted = false)
    {
        EnsureEntityColumn(whereColumn);
        var (setSql, param) = BuildPartialSet(patch);
        param.Add("whereValue", whereValue);
        var whereDeleted = requireNotDeleted ? " AND `IsDeleted` = 0" : string.Empty;
        var sql = $@"
            UPDATE `{TableName}`
            SET {setSql}, `UpdatedAt` = NOW()
            WHERE `{whereColumn}` = @whereValue{whereDeleted}";
        return ExecuteAsync(sql, param);
    }

    protected SqlFilterBuilder CreateFilter(bool softDeleteOnly = true)
        => new(AllMappedColumns, softDeleteOnly);

    protected Task<int> CountByFilterAsync(SqlFilterBuilder filter)
        => CountTableByFilterAsync(TableName, filter);

    protected Task<(IEnumerable<TEntity> Items, int TotalCount)> ListByFilterAsync(
        SqlFilterBuilder filter,
        string orderBy)
        => ListTableByFilterAsync<TEntity>(TableName, filter, orderBy);

    // ─── private ────────────────────────────────────────────────────────────

    private (string SetSql, DynamicParameters Param) BuildPartialSet(object patch)
    {
        var props = patch.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .ToList();

        if (props.Count == 0)
        {
            throw new InvalidOperationException("Partial update: patch không có property.");
        }

        var param = new DynamicParameters();
        var sets = new List<string>();

        foreach (var prop in props)
        {
            var name = prop.Name;
            if (!EntityColumnMapper.IsSafeIdentifier(name))
            {
                throw new InvalidOperationException($"Partial update: cột không hợp lệ '{name}'.");
            }

            // Chỉ cho phép cột thuộc entity (trừ Id)
            if (string.Equals(name, "Id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!HasColumn(name))
            {
                throw new InvalidOperationException(
                    $"Partial update: '{name}' không thuộc {typeof(TEntity).Name}.");
            }

            sets.Add($"`{name}` = @{name}");
            param.Add(name, prop.GetValue(patch));
        }

        if (sets.Count == 0)
        {
            throw new InvalidOperationException("Partial update: không còn cột để SET.");
        }

        return (string.Join(", ", sets), param);
    }

    private static string ResolveTableName()
    {
        var attr = typeof(TEntity).GetCustomAttribute<DbTableAttribute>();
        if (attr is null || string.IsNullOrWhiteSpace(attr.Name))
        {
            throw new InvalidOperationException(
                $"Entity {typeof(TEntity).Name} cần [DbTable(\"TenBang\")].");
        }

        if (!EntityColumnMapper.IsSafeIdentifier(attr.Name))
        {
            throw new InvalidOperationException($"Invalid [DbTable] name: {attr.Name}");
        }

        return attr.Name;
    }

    private void EnsureEntityColumn(string column)
    {
        if (!EntityColumnMapper.IsSafeIdentifier(column))
        {
            throw new InvalidOperationException($"Tên cột không hợp lệ: '{column}'");
        }

        var ok = HasColumn(column)
                 || EntityColumnMapper.SoftDeleteSystemColumns.Contains(column);
        if (!ok)
        {
            throw new InvalidOperationException(
                $"'{column}' không phải property của {typeof(TEntity).Name}.");
        }
    }
}

/// <summary>Alias tương thích — dùng <see cref="GenericRepository{TEntity}"/>.</summary>
public abstract class SoftDeleteRepository<TEntity> : GenericRepository<TEntity> where TEntity : class
{
    protected SoftDeleteRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }
}
