using EmployeeManagement.Application.DTOs;

namespace EmployeeManagement.Application.Interfaces;

/// <summary>
/// Tạo tài khoản đăng nhập EMPLOYEE kèm hồ sơ nhân viên (một transaction).
/// </summary>
public interface IEmployeeAccountService
{
    Task<ProvisionedEmployeeAccount> ProvisionAsync(ProvisionEmployeeAccountRequest request);
}
