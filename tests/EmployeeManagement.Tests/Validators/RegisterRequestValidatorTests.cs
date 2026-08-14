using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Validators;
using FluentAssertions;

namespace EmployeeManagement.Tests.Validators;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_email_is_invalid()
    {
        var request = new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "not-an-email",
            Password = "Abcdef1!",
            PhoneNumber = "0901234567"
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Should_pass_when_request_is_valid()
    {
        var request = new RegisterRequest
        {
            FullName = "Nguyen Van A",
            Email = "a.nguyen@example.com",
            Password = "Abcdef1!",
            ConfirmPassword = "Abcdef1!",
            PhoneNumber = "0901234567",
            DepartmentId = 1,
            PositionId = 1
        };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
