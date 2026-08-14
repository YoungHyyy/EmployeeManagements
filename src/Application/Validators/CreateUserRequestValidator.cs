using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có chữ hoa")
            .Matches("[a-z]").WithMessage("Mật khẩu phải có chữ thường")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có số");

        RuleFor(x => x.PhoneNumber)
            .Matches(ValidationRules.VietnamPhone)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage(ValidationRules.VietnamPhoneMessage);

        RuleFor(x => x.RoleCode)
            .Must(code =>
            {
                var normalized = string.IsNullOrWhiteSpace(code)
                    ? "EMPLOYEE"
                    : code.Trim().ToUpperInvariant();
                return normalized is "ADMIN" or "EMPLOYEE";
            })
            .WithMessage("Role không hợp lệ. Chỉ chấp nhận ADMIN hoặc EMPLOYEE");
    }
}
