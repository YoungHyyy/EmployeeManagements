using EmployeeManagement.Application.DTOs;
using FluentValidation;

namespace EmployeeManagement.Application.Validators;

public class DepartmentDtoValidator : AbstractValidator<DepartmentDto>
{
    public DepartmentDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Mã phòng ban là bắt buộc")
            .MaximumLength(20).WithMessage("Mã phòng ban tối đa 20 ký tự");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên phòng ban là bắt buộc")
            .MaximumLength(150).WithMessage("Tên phòng ban tối đa 150 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Mô tả tối đa 500 ký tự");
    }
}
