using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class ProfileUpdateRequestValidator : AbstractValidator<ProfileUpdateRequest>
{
    public ProfileUpdateRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự");

        RuleFor(x => x.PhoneNumber)
            .Matches(@"^(0)(3|5|7|8|9)\d{8}$")
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Số điện thoại không đúng định dạng Việt Nam");

        RuleFor(x => x.Gender)
            .Must(g => string.IsNullOrWhiteSpace(g) || new[] { "Male", "Female", "Other" }.Contains(g))
            .WithMessage("Giới tính không hợp lệ");

        RuleFor(x => x.Address)
            .MaximumLength(500).WithMessage("Địa chỉ tối đa 500 ký tự");
    }
}
