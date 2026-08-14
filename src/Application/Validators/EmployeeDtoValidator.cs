using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeDtoValidator(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không đúng định dạng")
            .MustAsync(BeUniqueEmailAsync)
            .WithMessage("Email đã tồn tại trong hệ thống");

        RuleFor(x => x.PhoneNumber)
            .Matches(ValidationRules.VietnamPhone)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage(ValidationRules.VietnamPhoneMessage);

        RuleFor(x => x.DepartmentId)
            .GreaterThan(0).WithMessage("Phòng ban là bắt buộc");

        RuleFor(x => x.PositionId)
            .GreaterThan(0).WithMessage("Chức vụ là bắt buộc");

        RuleFor(x => x.HireDate)
            .NotEmpty().WithMessage("Ngày vào làm là bắt buộc");

        RuleFor(x => x.Gender)
            .Must(g => string.IsNullOrWhiteSpace(g) || new[] { "Male", "Female", "Other" }.Contains(g))
            .WithMessage("Giới tính không hợp lệ");

        RuleFor(x => x.Status)
            .Must(s => string.IsNullOrWhiteSpace(s) || new[] { "Working", "Probation", "Resigned", "OnLeave" }.Contains(s))
            .WithMessage("Trạng thái làm việc không hợp lệ");
    }

    /// <summary>
    /// Create: Id = 0 → email must not exist.
    /// Update: Id > 0 → email may match current employee (exclude Id).
    /// </summary>
    private async Task<bool> BeUniqueEmailAsync(EmployeeDto dto, string email, CancellationToken cancellation)
    {
        var excludeId = dto.Id > 0 ? dto.Id : (int?)null;
        var exists = await _employeeRepository.ExistsByEmailAsync(email, excludeId);
        return !exists;
    }
}
