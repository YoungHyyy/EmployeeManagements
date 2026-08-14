using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace EmployeeManagement.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmailAlreadyExists()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository
            .Setup(x => x.ExistsByEmailAsync("existing@mail.com", null))
            .ReturnsAsync(true);

        var roleRepository = new Mock<IRoleRepository>();
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-123456",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagementClient",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        var tokenService = new TokenService(configuration);

        var service = new AuthService(
            userRepository.Object,
            roleRepository.Object,
            refreshTokenRepository.Object,
            tokenService);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "existing@mail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Email đã tồn tại");
        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordsDoNotMatch()
    {
        var service = CreateService();

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "new@mail.com",
            Password = "Password123!",
            ConfirmPassword = "Different123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không khớp");
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnFailure_WhenEmployeeRoleIsMissing()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("new@mail.com")).ReturnsAsync((User?)null);

        var roleRepository = new Mock<IRoleRepository>();
        roleRepository.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync((int?)null);

        var service = CreateService(userRepository.Object, refreshTokenRepository: null, roleRepository: roleRepository.Object);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            FullName = "Test User",
            Email = "new@mail.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!",
            PhoneNumber = "0901234567"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Không tìm thấy role EMPLOYEE");
        userRepository.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        userRepository.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserNotFound()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("missing@mail.com")).ReturnsAsync((User?)null);

        var service = CreateService(userRepository.Object);

        var result = await service.LoginAsync(new LoginRequest { Email = "missing@mail.com", Password = "Password123!" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không hợp lệ");
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnFailure_WhenUserIsInactive()
    {
        var password = "Password123!";
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("locked@mail.com")).ReturnsAsync(new User
        {
            Id = 7,
            Email = "locked@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            IsActive = false
        });

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.LoginAsync(new LoginRequest { Email = "locked@mail.com", Password = password });

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Tài khoản đã bị vô hiệu hóa");
        result.Data.Should().BeNull();
        refreshTokenRepository.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnInvalidCredentials_WhenInactiveUserHasWrongPassword()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("locked@mail.com")).ReturnsAsync(new User
        {
            Id = 7,
            Email = "locked@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!"),
            IsActive = false
        });

        var service = CreateService(userRepository.Object);

        var result = await service.LoginAsync(new LoginRequest { Email = "locked@mail.com", Password = "WrongPassword123!" });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không hợp lệ");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnFailure_WhenCurrentPasswordIsIncorrect()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!") });

        var service = CreateService(userRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("không đúng");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenPasswordIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!") });
        userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(x => x.RevokeAllUserTokensAsync(1)).Returns(Task.CompletedTask);

        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("thành công");
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(1), Times.Once);
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldNotRevokeTokens_WhenCurrentPasswordIsIncorrect()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User
        {
            Id = 1,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword123!")
        });

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeFalse();
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ResetPasswordAsync_ShouldRevokeRefreshTokens_WhenOtpIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByEmailAsync("user@mail.com")).ReturnsAsync(new User
        {
            Id = 4,
            Email = "user@mail.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!"),
            OtpCode = "123456",
            OtpExpiration = DateTime.UtcNow.AddMinutes(5)
        });
        userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var refreshTokenRepository = new Mock<IRefreshTokenRepository>();
        refreshTokenRepository.Setup(x => x.RevokeAllUserTokensAsync(4)).Returns(Task.CompletedTask);

        var service = CreateService(userRepository.Object, refreshTokenRepository.Object);

        var result = await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = "user@mail.com",
            OtpCode = "123456",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("thành công");
        refreshTokenRepository.Verify(x => x.RevokeAllUserTokensAsync(4), Times.Once);
    }

    private static AuthService CreateService(
        IUserRepository? userRepository = null,
        IRefreshTokenRepository? refreshTokenRepository = null,
        IRoleRepository? roleRepository = null)
    {
        userRepository ??= new Mock<IUserRepository>().Object;
        roleRepository ??= new Mock<IRoleRepository>().Object;
        refreshTokenRepository ??= new Mock<IRefreshTokenRepository>().Object;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-123456",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagementClient",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        var tokenService = new TokenService(configuration);
        return new AuthService(userRepository, roleRepository, refreshTokenRepository, tokenService);
    }
}
