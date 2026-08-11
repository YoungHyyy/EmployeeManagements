using EmployeeManagement.Domain.Attributes;

namespace EmployeeManagement.Domain.Entities;

/// <summary>CRUD cột auto qua GenericRepository (Dapper+Reflection) — thêm field chỉ sửa entity + DB.</summary>
[DbTable("Employees")]
public class Employee
{
    public int Id { get; set; }

    [DbUpdateIgnore]
    public int? UserId { get; set; }

    public int DepartmentId { get; set; }
    public int PositionId { get; set; }

    [DbUpdateIgnore]
    public string EmployeeCode { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public DateTime HireDate { get; set; }
    public string? Status { get; set; }
    public decimal? Salary { get; set; }
    public DateTime? ContractStartDate { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}
