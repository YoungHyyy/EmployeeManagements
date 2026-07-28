using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class EmployeeDtoValidator : AbstractValidator<EmployeeDto>
{
    public EmployeeDtoValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(0)(3[2-9]|5[6|8|9]|7[0|6-9]|8[1-9]|9[0-9])[0-9]{7}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Số điện thoại không đúng định dạng Việt Nam");

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
}
