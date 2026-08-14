namespace EmployeeManagement.Application.DTOs;

public class ProfileDto
{
    public int Id { get; set; }
    public int? EmployeeId { get; set; }
    public string? EmployeeCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public int? DepartmentId { get; set; }
    public int? PositionId { get; set; }
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public DateTime? HireDate { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
