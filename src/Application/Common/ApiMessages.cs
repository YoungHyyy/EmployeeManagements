namespace EmployeeManagement.Application.Common;

/// <summary>
/// Thông báo API thống nhất (tiếng Việt) — Controller / Service / Middleware dùng chung.
/// </summary>
public static class ApiMessages
{
    // ── Chung ──────────────────────────────────────────────────────────────
    public const string Success = "Thao tác thành công";
    public const string Created = "Tạo mới thành công";
    public const string Updated = "Cập nhật thành công";
    public const string Deleted = "Xóa thành công";
    public const string Restored = "Khôi phục thành công";
    public const string InvalidRequest = "Yêu cầu không hợp lệ";
    public const string ValidationFailed = "Dữ liệu không hợp lệ";
    public const string Unauthorized = "Chưa đăng nhập hoặc token không hợp lệ";
    public const string Forbidden = "Bạn không có quyền thực hiện thao tác này";
    public const string NotFound = "Không tìm thấy dữ liệu";
    public const string Conflict = "Dữ liệu đã tồn tại trong hệ thống";
    public const string SystemError = "Đã xảy ra lỗi hệ thống";

    // ── Auth ───────────────────────────────────────────────────────────────
    public const string RegisterSuccess = "Đăng ký thành công";
    public const string LoginSuccess = "Đăng nhập thành công";
    public const string LoginFailed = "Thông tin đăng nhập không hợp lệ";
    public const string AccountDisabled = "Tài khoản đã bị vô hiệu hóa";
    public const string LogoutSuccess = "Đăng xuất thành công";
    public const string RefreshSuccess = "Làm mới token thành công";
    public const string RefreshInvalid = "Refresh token không hợp lệ hoặc đã hết hạn";
    public const string PasswordChanged = "Đổi mật khẩu thành công";
    public const string PasswordReset = "Đặt lại mật khẩu thành công";
    public const string PasswordMismatch = "Mật khẩu và mật khẩu xác nhận không khớp";
    public const string NewPasswordMismatch = "Mật khẩu mới và xác nhận mật khẩu không khớp";
    public const string CurrentPasswordWrong = "Mật khẩu hiện tại không đúng";
    public const string EmailExists = "Email đã tồn tại";
    public const string EmailNotFound = "Không tìm thấy người dùng với email này";
    public const string OtpInvalid = "Mã OTP không hợp lệ";
    public const string OtpExpired = "Mã OTP đã hết hạn";
    public static string OtpCreated(string otp) => $"Mã OTP đã được tạo (giả lập): {otp}";

    // ── User ───────────────────────────────────────────────────────────────
    public const string UserNotFound = "Không tìm thấy người dùng";
    public const string UserCreated = "Tạo người dùng thành công";
    public const string UserUpdated = "Cập nhật người dùng thành công";
    public const string UserDeleted = "Xóa người dùng thành công";
    public const string UserCannotDeleteSelf = "Không thể xóa tài khoản của chính mình";
    public const string RoleInvalid = "Role không hợp lệ. Chỉ chấp nhận ADMIN hoặc EMPLOYEE";
    public static string RoleNotFound(string role) => $"Không tìm thấy role {role} trong hệ thống";
    public const string DefaultCatalogMissing =
        "Hệ thống chưa có phòng ban/chức vụ mặc định (UNASSIGNED / STAFF). Liên hệ Admin.";

    // ── Employee ───────────────────────────────────────────────────────────
    public const string EmployeeNotFound = "Không tìm thấy nhân viên";
    public const string EmployeeCreated = "Tạo nhân viên thành công";
    public const string EmployeeUpdated = "Cập nhật nhân viên thành công";
    public const string EmployeeDeleted = "Xóa nhân viên thành công";
    public const string EmployeeRestored = "Khôi phục nhân viên thành công";
    public const string EmployeeEmailExists = "Email đã tồn tại trong hệ thống";

    // ── Department ─────────────────────────────────────────────────────────
    public const string DepartmentNotFound = "Không tìm thấy phòng ban";
    public const string DepartmentCreated = "Tạo phòng ban thành công";
    public const string DepartmentUpdated = "Cập nhật phòng ban thành công";
    public const string DepartmentDeleted = "Xóa phòng ban thành công";
    public const string DepartmentHasEmployees = "Không thể xóa phòng ban khi vẫn còn nhân viên liên kết";
    public static string DepartmentCodeExists(string code) => $"Mã phòng ban '{code}' đã tồn tại";
    public static string DepartmentNameExists(string name) => $"Tên phòng ban '{name}' đã tồn tại";

    // ── Position ───────────────────────────────────────────────────────────
    public const string PositionNotFound = "Không tìm thấy chức vụ";
    public const string PositionCreated = "Tạo chức vụ thành công";
    public const string PositionUpdated = "Cập nhật chức vụ thành công";
    public const string PositionDeleted = "Xóa chức vụ thành công";
    public const string PositionHasEmployees = "Không thể xóa chức vụ khi vẫn còn nhân viên liên kết";
    public static string PositionCodeExists(string code) => $"Mã chức vụ '{code}' đã tồn tại";
    public static string PositionNameExists(string name) => $"Tên chức vụ '{name}' đã tồn tại";

    // ── Profile ────────────────────────────────────────────────────────────
    public const string ProfileNotFound = "Không tìm thấy hồ sơ người dùng";
    public const string ProfileUpdated = "Cập nhật hồ sơ thành công";
    public const string AvatarUploaded = "Tải ảnh đại diện thành công";
    public const string AvatarInvalid = "Vui lòng chọn file ảnh hợp lệ.";
    public const string AvatarTooLarge = "Ảnh vượt quá giới hạn 2MB.";
    public const string AvatarFormat = "Chỉ hỗ trợ định dạng .jpg, .jpeg hoặc .png.";

    // ── Dashboard / Audit ──────────────────────────────────────────────────
    public const string DashboardOk = "Lấy thống kê dashboard thành công";
    public const string AuditListOk = "Lấy danh sách audit log thành công";
    public const string ListOk = "Lấy danh sách thành công";
    public const string DetailOk = "Lấy chi tiết thành công";
}
