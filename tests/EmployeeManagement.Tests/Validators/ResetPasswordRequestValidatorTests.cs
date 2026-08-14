using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Validators;
using FluentAssertions;

namespace EmployeeManagement.Tests.Validators;

public class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_password_is_only_six_digits()
    {
        var result = _validator.Validate(ValidRequest(newPassword: "123456"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword"
            && e.ErrorMessage == "Mật khẩu tối thiểu 8 ký tự");
    }

    [Fact]
    public void Should_fail_when_password_lacks_complexity()
    {
        var result = _validator.Validate(ValidRequest(newPassword: "abcdefgh"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword"
            && e.ErrorMessage == "Mật khẩu phải có chữ hoa");
        result.Errors.Should().Contain(e => e.PropertyName == "NewPassword"
            && e.ErrorMessage == "Mật khẩu phải có số");
    }

    [Fact]
    public void Should_pass_when_password_meets_policy()
    {
        var result = _validator.Validate(ValidRequest(newPassword: "NewPassword123"));

        result.IsValid.Should().BeTrue();
    }

    private static ResetPasswordRequest ValidRequest(string newPassword) => new()
    {
        Email = "user@mail.com",
        OtpCode = "123456",
        NewPassword = newPassword,
        ConfirmPassword = newPassword
    };
}
