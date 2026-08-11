namespace EmployeeManagement.Application.Common;

public static class AuditActions
{
    public const string LoginSuccess = "LOGIN_SUCCESS";
    public const string LoginFailed = "LOGIN_FAILED";
    public const string Logout = "LOGOUT";
    public const string Register = "REGISTER";
    public const string ChangePassword = "CHANGE_PASSWORD";
    public const string ResetPassword = "RESET_PASSWORD";
    public const string ForgotPassword = "FORGOT_PASSWORD";
    public const string Create = "CREATE";
    public const string Update = "UPDATE";
    public const string SoftDelete = "SOFT_DELETE";
    public const string Restore = "RESTORE";
}

public static class AuditModules
{
    public const string Auth = "Auth";
    public const string Employee = "Employee";
    public const string User = "User";
    public const string Department = "Department";
    public const string Position = "Position";
    public const string Profile = "Profile";
}
