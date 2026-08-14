using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.DTOs;

/// <summary>Tạo cặp User (role EMPLOYEE) + Employee trong một transaction.</summary>
public class ProvisionEmployeeAccountRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? Status { get; set; }
}

public class ProvisionedEmployeeAccount
{
    public int UserId { get; init; }
    public Employee Employee { get; init; } = null!;
}
