namespace EmployeeManagement.Application.DTOs
{
    public class UpdateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
    }
}
