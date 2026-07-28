namespace EmployeeManagement.Application.DTOs;

public class RoleAssignmentRequest
{
    public int UserId { get; set; }
    public string RoleCode { get; set; } = "EMPLOYEE";
}
