using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using EmployeeManagement.Application.Interfaces;

namespace EmployeeManagement.Infrastructure.Repositories;

public abstract class BaseRepository<T> where T : class
{
    private readonly IDbConnectionFactory _dbFactory;

    protected BaseRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected IDbConnectionFactory DbFactory => _dbFactory;

    protected async Task<int> InsertAsync(string sql, object? param = null)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<int>(sql, param);
    }

    protected async Task ExecuteAsync(string sql, object? param = null)
    {
        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(sql, param);
    }

    protected async Task<T?> QuerySingleAsync(string sql, object? param = null)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    protected async Task<TResult?> QueryScalarAsync<TResult>(string sql, object? param = null)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.ExecuteScalarAsync<TResult?>(sql, param);
    }

    protected async Task<IEnumerable<T>> QueryListAsync(string sql, object? param = null)
    {
        using var conn = _dbFactory.CreateConnection();
        return await conn.QueryAsync<T>(sql, param);
    }
}
