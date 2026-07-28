using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagement.Api.Authentication;

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string EmployeeOrAdmin = "EmployeeOrAdmin";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AdminOnly, policy => policy.RequireRole("ADMIN"));
        options.AddPolicy(EmployeeOrAdmin, policy => policy.RequireRole("ADMIN", "EMPLOYEE"));
    }
}
