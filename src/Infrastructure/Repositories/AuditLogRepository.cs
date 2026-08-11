using Dapper;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly IDbConnectionFactory _dbFactory;

    public AuditLogRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task AddAsync(AuditLog log)
    {
        const string sql = @"
            INSERT INTO AuditLogs
                (UserId, Action, Module, EntityName, EntityId, IpAddress, RequestPath, Details, CreatedAt)
            VALUES
                (@UserId, @Action, @Module, @EntityName, @EntityId, @IpAddress, @RequestPath, @Details, @CreatedAt);";

        using var conn = _dbFactory.CreateConnection();
        await conn.ExecuteAsync(sql, log);
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> ListAsync(AuditLogQuery query)
    {
        var where = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (query.UserId.HasValue)
        {
            where += " AND UserId = @UserId";
            parameters.Add("UserId", query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            where += " AND Action = @Action";
            parameters.Add("Action", query.Action.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(query.Module))
        {
            where += " AND Module = @Module";
            parameters.Add("Module", query.Module.Trim());
        }

        if (query.From.HasValue)
        {
            where += " AND CreatedAt >= @From";
            parameters.Add("From", query.From.Value);
        }

        if (query.To.HasValue)
        {
            where += " AND CreatedAt <= @To";
            parameters.Add("To", query.To.Value);
        }

        var offset = (query.Page - 1) * query.PageSize;
        parameters.Add("Limit", query.PageSize);
        parameters.Add("Offset", offset);

        var countSql = $"SELECT COUNT(1) FROM AuditLogs {where}";
        var listSql = $@"
            SELECT Id, UserId, Action, Module, EntityName, EntityId, IpAddress, RequestPath, Details, CreatedAt
            FROM AuditLogs
            {where}
            ORDER BY CreatedAt DESC, Id DESC
            LIMIT @Limit OFFSET @Offset";

        using var conn = _dbFactory.CreateConnection();
        var total = await conn.ExecuteScalarAsync<int>(countSql, parameters);
        var items = await conn.QueryAsync<AuditLog>(listSql, parameters);
        return (items, total);
    }
}
