using System.Data;
using Dapper;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// Base Dapper (không EF): connection + Execute/Query.
/// Tầng dưới của <see cref="GenericRepository{TEntity}"/>.
/// SQL chỉ nằm Infrastructure (backend.md).
/// </summary>
public abstract class BaseRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    protected BaseRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected IDbConnectionFactory DbFactory => _dbFactory;

    protected IDbConnection CreateConnection() => _dbFactory.CreateConnection();

    /// <summary>
    /// Một connection + transaction. Lỗi → rollback, không để bản ghi dở.
    /// </summary>
    protected async Task<T> WithTransactionAsync<T>(Func<IDbConnection, IDbTransaction, Task<T>> action)
    {
        using var conn = CreateConnection();
        if (conn.State != ConnectionState.Open)
        {
            conn.Open();
        }

        using var tx = conn.BeginTransaction();
        try
        {
            var result = await action(conn, tx);
            tx.Commit();
            return result;
        }
        catch
        {
            try
            {
                tx.Rollback();
            }
            catch
            {
                // Transaction may already be aborted by the provider.
            }

            throw;
        }
    }

    protected async Task<int> ExecuteAsync(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteAsync(sql, param);
    }

    protected async Task<T?> ExecuteScalarAsync<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<T?>(sql, param);
    }

    protected async Task<int> InsertAsync(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, param);
    }

    protected async Task<T?> QuerySingleAsync<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    protected async Task<IEnumerable<T>> QueryListAsync<T>(string sql, object? param = null)
    {
        using var conn = CreateConnection();
        return await conn.QueryAsync<T>(sql, param);
    }

    /// <summary>INSERT tự sinh cột từ entity (không soft-delete). Trả LAST_INSERT_ID nếu có.</summary>
    protected Task<int> InsertMappedAsync<TEntity>(
        string tableName,
        TEntity entity,
        IReadOnlyList<string> insertColumns,
        bool returnLastId = true) where TEntity : class
    {
        EntityColumnMapper.EnsureSafeIdentifiers(insertColumns);
        if (!EntityColumnMapper.IsSafeIdentifier(tableName))
        {
            throw new InvalidOperationException($"Invalid table: {tableName}");
        }

        var cols = string.Join(", ", insertColumns.Select(c => $"`{c}`"));
        var vals = string.Join(", ", insertColumns.Select(c => "@" + c));
        var sql = returnLastId
            ? $@"INSERT INTO `{tableName}` ({cols}) VALUES ({vals}); SELECT LAST_INSERT_ID();"
            : $@"INSERT INTO `{tableName}` ({cols}) VALUES ({vals}); SELECT 0;";

        return InsertAsync(sql, entity);
    }

    /// <summary>COUNT + SELECT * page theo SqlFilterBuilder (1 connection).</summary>
    protected async Task<(IEnumerable<T> Items, int TotalCount)> ListTableByFilterAsync<T>(
        string tableName,
        SqlFilterBuilder filter,
        string orderBy)
    {
        if (!EntityColumnMapper.IsSafeIdentifier(tableName))
        {
            throw new InvalidOperationException($"Invalid table: {tableName}");
        }

        using var conn = CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM `{tableName}` {filter.WhereSql}",
            filter.Parameters);
        var items = await conn.QueryAsync<T>(
            $@"SELECT * FROM `{tableName}`
               {filter.WhereSql}
               ORDER BY {orderBy}
               LIMIT @limit OFFSET @offset",
            filter.Parameters);
        return (items, total);
    }

    protected async Task<int> CountTableByFilterAsync(string tableName, SqlFilterBuilder filter)
    {
        if (!EntityColumnMapper.IsSafeIdentifier(tableName))
        {
            throw new InvalidOperationException($"Invalid table: {tableName}");
        }

        var n = await ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM `{tableName}` {filter.WhereSql}",
            filter.Parameters);
        return n;
    }
}

/// <summary>Base gõ entity — QuerySingle/List không lặp T.</summary>
public abstract class BaseRepository<T> : BaseRepository where T : class
{
    protected BaseRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    protected Task<T?> QuerySingleAsync(string sql, object? param = null)
        => QuerySingleAsync<T>(sql, param);

    protected Task<IEnumerable<T>> QueryListAsync(string sql, object? param = null)
        => QueryListAsync<T>(sql, param);

    protected Task<TResult?> QueryScalarAsync<TResult>(string sql, object? param = null)
        => ExecuteScalarAsync<TResult>(sql, param);
}
