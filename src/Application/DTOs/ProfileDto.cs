namespace EmployeeManagement.Application.DTOs;

public class ProfileDto
{
    /// <summary>User.Id — khớp NameIdentifier trên JWT.</summary>
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public int DepartmentId { get; set; }
    public int PositionId { get; set; }
    public DateTime HireDate { get; set; }
    public string? Status { get; set; }
    /// <summary>Chỉ lấy từ Users.AvatarUrl.</summary>
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
