namespace EmployeeManagement.Application.DTOs
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        /// <summary>Bắt buộc — Employees.DepartmentId NOT NULL. Không gán phòng ban mặc định.</summary>
        public int DepartmentId { get; set; }
        /// <summary>Bắt buộc — Employees.PositionId NOT NULL.</summary>
        public int PositionId { get; set; }
        /// <summary>Không gửi thì lấy ngày hiện tại (UTC).</summary>
        public DateTime? HireDate { get; set; }
    }
}
