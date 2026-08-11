namespace EmployeeManagement.Domain.Entities;

public class AuditLog
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
