using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại là bắt buộc");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới là bắt buộc")
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches("[A-Z]").WithMessage("Mật khẩu phải có chữ hoa")
            .Matches("[a-z]").WithMessage("Mật khẩu phải có chữ thường")
            .Matches("[0-9]").WithMessage("Mật khẩu phải có số");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp");
    }
}
