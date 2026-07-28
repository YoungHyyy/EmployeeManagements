using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Validators;
using FluentAssertions;

namespace EmployeeManagement.Tests.Validators;

public class EmployeeDtoValidatorTests
{
    private readonly EmployeeDtoValidator _validator = new();

    [Fact]
    public void Should_fail_when_required_fields_are_missing()
    {
        var request = new EmployeeDto
        {
            FullName = "",
            Email = "not-an-email",
            DepartmentId = 0,
            PositionId = 0,
            HireDate = DateTime.Today.AddDays(-1)
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "DepartmentId");
        result.Errors.Should().Contain(e => e.PropertyName == "PositionId");
    }

    [Fact]
    public void Should_pass_when_employee_request_is_valid()
    {
        var request = new EmployeeDto
        {
            FullName = "Nguyen Van A",
            Email = "a.nguyen@example.com",
            PhoneNumber = "0901234567",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today.AddDays(-30),
            Gender = "Male",
            Status = "Working"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
