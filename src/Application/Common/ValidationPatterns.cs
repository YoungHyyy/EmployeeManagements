namespace EmployeeManagement.Application.Common;

public static class ValidationPatterns
{
    /// <summary>SĐT di động VN 10 số: 03x/05x/07x/08x/09x.</summary>
    public const string VietnamPhone = @"^(0)(3|5|7|8|9)\d{8}$";
    public const string VietnamPhoneMessage = "Số điện thoại không đúng định dạng Việt Nam";
}
