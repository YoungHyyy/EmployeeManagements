using System.Reflection;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Attributes;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// Append-only: INSERT map entity (Reflection + Dapper). List = filter đặc thù.
/// Không EF, không soft-delete full CRUD.
/// </summary>
public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
{
    private static readonly string Table =
        typeof(AuditLog).GetCustomAttribute<DbTableAttribute>()?.Name
        ?? "AuditLogs";

    private static readonly EntityColumnMapper.ColumnMap Columns =
        EntityColumnMapper.GetMap(typeof(AuditLog), EntityColumnMapper.IdentityOnlySystemColumns);

    public AuditLogRepository(IDbConnectionFactory dbFactory) : base(dbFactory)
    {
    }

    public Task AddAsync(AuditLog log)
        => InsertMappedAsync(Table, log, Columns.InsertColumns, returnLastId: false);

    public Task<(IEnumerable<AuditLog> Items, int TotalCount)> ListAsync(AuditLogQuery query)
    {
        var filter = new SqlFilterBuilder(Columns.AllColumns, softDeleteOnly: false)
            .Equal(nameof(AuditLog.UserId), query.UserId)
            .EqualUpper(nameof(AuditLog.Action), query.Action)
            .Equal(nameof(AuditLog.Module), query.Module)
            .GreaterOrEqual(nameof(AuditLog.CreatedAt), query.From)
            .LessOrEqual(nameof(AuditLog.CreatedAt), query.To)
            .AddPaging(query.Page, query.PageSize);

        return ListTableByFilterAsync<AuditLog>(Table, filter, "`CreatedAt` DESC, `Id` DESC");
    }
}
