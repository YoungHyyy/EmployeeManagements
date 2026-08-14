namespace EmployeeManagement.Application.Common;

/// <summary>Quy tắc validate dùng chung (backend.md §9).</summary>
public static class ValidationRules
{
    /// <summary>SĐT Việt Nam: 10 số, đầu 03/05/07/08/09.</summary>
    public const string VietnamPhone = @"^(0)(3|5|7|8|9)\d{8}$";
    public const string VietnamPhoneMessage = "Số điện thoại không đúng định dạng Việt Nam";
}
