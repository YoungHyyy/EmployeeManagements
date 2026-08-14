using EmployeeManagement.Application.Common;
using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên là bắt buộc")
            .MaximumLength(200).WithMessage("Họ tên tối đa 200 ký tự");

        RuleFor(x => x.PhoneNumber)
            .Matches(ValidationRules.VietnamPhone)
            .When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage(ValidationRules.VietnamPhoneMessage);
    }
}
