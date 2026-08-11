using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Application.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IAuditLogRepository repository, ILogger<AuditLogService>? logger = null)
    {
        _repository = repository;
        _logger = logger ?? NullLogger<AuditLogService>.Instance;
    }

    public async Task WriteAsync(AuditLogWriteRequest request)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Action))
        {
            return;
        }

        try
        {
            var log = new AuditLog
            {
                UserId = request.UserId,
                Action = request.Action.Trim().ToUpperInvariant(),
                Module = string.IsNullOrWhiteSpace(request.Module) ? null : request.Module.Trim(),
                EntityName = request.EntityName,
                EntityId = request.EntityId,
                IpAddress = request.IpAddress,
                RequestPath = request.RequestPath,
                Details = request.Details,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(log);
        }
        catch (Exception ex)
        {
            // Never fail the main business operation because of audit write.
            _logger.LogError(ex, "Ghi AuditLog thất bại: {Action} / {Module}", request.Action, request.Module);
        }
    }

    public async Task<AuditLogListResult> GetListAsync(AuditLogQuery query)
    {
        query.Page = query.Page < 1 ? 1 : query.Page;
        query.PageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var (items, total) = await _repository.ListAsync(query);

        return new AuditLogListResult
        {
            Items = items.Select(Map).ToList(),
            Page = query.Page,
            PageSize = query.PageSize,
            TotalCount = total
        };
    }

    private static AuditLogDto Map(AuditLog log) => new()
    {
        Id = log.Id,
        UserId = log.UserId,
        Action = log.Action,
        Module = log.Module,
        EntityName = log.EntityName,
        EntityId = log.EntityId,
        IpAddress = log.IpAddress,
        RequestPath = log.RequestPath,
        Details = log.Details,
        CreatedAt = log.CreatedAt
    };
}
