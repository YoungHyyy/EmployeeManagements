namespace EmployeeManagement.Application.DTOs;

public class AuditLogDto
{
    public long Id { get; set; }
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Module { get; set; }
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestPath { get; set; }
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogWriteRequest
{
    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? RequestPath { get; set; }
    public string? Details { get; set; }
}

public class AuditLogListResult
{
    public IReadOnlyList<AuditLogDto> Items { get; set; } = Array.Empty<AuditLogDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public class AuditLogQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int? UserId { get; set; }
    public string? Action { get; set; }
    public string? Module { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}
