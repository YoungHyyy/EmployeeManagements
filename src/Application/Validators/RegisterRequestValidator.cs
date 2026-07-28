using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(200);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email là bắt buộc")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(0)(3|5|7|8|9)\d{8}$").WithMessage("Số điện thoại không đúng định dạng Việt Nam");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu là bắt buộc")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có chữ hoa")
            .Matches("[a-z]").WithMessage("Mật khẩu phải có chữ thường")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có số");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu là bắt buộc")
            .Equal(x => x.Password).WithMessage("Mật khẩu và xác nhận mật khẩu không khớp");
    }
}
