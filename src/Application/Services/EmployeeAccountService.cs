using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;

namespace EmployeeManagement.Application.Services;

public class EmployeeAccountService : IEmployeeAccountService
{
    private readonly IUserRepository _userRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPositionRepository _positionRepository;

    public EmployeeAccountService(
        IUserRepository userRepository,
        IEmployeeRepository employeeRepository,
        IRoleRepository roleRepository,
        IDepartmentRepository departmentRepository,
        IPositionRepository positionRepository)
    {
        _userRepository = userRepository;
        _employeeRepository = employeeRepository;
        _roleRepository = roleRepository;
        _departmentRepository = departmentRepository;
        _positionRepository = positionRepository;
    }

    public async Task<ProvisionedEmployeeAccount> ProvisionAsync(ProvisionEmployeeAccountRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidOperationException(ApiMessages.EmployeePasswordRequired);
        }

        if (await _userRepository.GetByEmailAsync(request.Email) != null
            || await _employeeRepository.ExistsByEmailAsync(request.Email))
        {
            throw new InvalidOperationException(ApiMessages.EmailExists);
        }

        _ = await _departmentRepository.GetByIdAsync(request.DepartmentId)
            ?? throw new KeyNotFoundException(ApiMessages.DepartmentNotFound);

        _ = await _positionRepository.GetByIdAsync(request.PositionId)
            ?? throw new KeyNotFoundException(ApiMessages.PositionNotFound);

        var roleId = await _roleRepository.GetRoleIdByCodeAsync("EMPLOYEE");
        if (!roleId.HasValue)
        {
            throw new InvalidOperationException(ApiMessages.RoleNotFound("EMPLOYEE"));
        }

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        var employee = new Employee
        {
            EmployeeCode = GenerateEmployeeCode(),
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            HireDate = request.HireDate == default ? DateTime.UtcNow.Date : request.HireDate,
            Status = string.IsNullOrWhiteSpace(request.Status) ? "Working" : request.Status,
            IsActive = true
        };

        var (userId, employeeId) = await _userRepository.CreateWithRoleAndEmployeeAsync(user, roleId.Value, employee);
        employee.UserId = userId;
        employee.Id = employeeId;

        return new ProvisionedEmployeeAccount
        {
            UserId = userId,
            Employee = employee
        };
    }

    private static string GenerateEmployeeCode()
    {
        var date = DateTime.UtcNow.ToString("yyMMdd");
        var random = Random.Shared.Next(1000, 9999);
        return $"EMP{date}{random}";
    }
}
