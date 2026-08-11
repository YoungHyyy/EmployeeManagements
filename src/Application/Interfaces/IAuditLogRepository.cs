using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log);
    Task<(IEnumerable<AuditLog> Items, int TotalCount)> ListAsync(AuditLogQuery query);
}
