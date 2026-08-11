using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

public interface IAuditLogService
{
    /// <summary>
    /// Best-effort write: failures are logged and never thrown to callers.
    /// </summary>
    Task WriteAsync(AuditLogWriteRequest request);

    Task<AuditLogListResult> GetListAsync(AuditLogQuery query);
}
