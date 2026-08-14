namespace EmployeeManagement.Application.DTOs
{
    public class CreateUserRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string RoleCode { get; set; } = "EMPLOYEE";
        /// <summary>Bắt buộc khi RoleCode = EMPLOYEE (tạo kèm hồ sơ nhân viên).</summary>
        public int DepartmentId { get; set; }
        public int PositionId { get; set; }
        public DateTime? HireDate { get; set; }
    }
}
