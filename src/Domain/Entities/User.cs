using EmployeeManagement.Domain.Attributes;

namespace EmployeeManagement.Domain.Entities;

[DbTable("Users")]
public class User
{
    public int Id { get; set; }

    [DbInsertIgnore]
    [DbUpdateIgnore]
    public string UserName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiration { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
