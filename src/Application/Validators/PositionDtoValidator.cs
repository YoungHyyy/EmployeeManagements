using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class PositionDtoValidator : AbstractValidator<PositionDto>
{
    public PositionDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã chức vụ là bắt buộc")
            .MaximumLength(20).WithMessage("Mã chức vụ tối đa 20 ký tự");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên chức vụ là bắt buộc")
            .MaximumLength(150).WithMessage("Tên chức vụ tối đa 150 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự");

        RuleFor(x => x.Level)
            .GreaterThanOrEqualTo(1).When(x => x.Level.HasValue)
            .WithMessage("Cấp bậc phải lớn hơn hoặc bằng 1");
    }
}
