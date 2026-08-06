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
            .Setup(x => x.GetByEmailAsync("existing@mail.com"))
            .ReturnsAsync(new User { Id = 1, Email = "existing@mail.com" });

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
        result.Message.Should().Contain("incorrect");
    }

    [Fact]
    public async Task ChangePasswordAsync_ShouldReturnSuccess_WhenPasswordIsValid()
    {
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new User { Id = 1, PasswordHash = BCrypt.Net.BCrypt.HashPassword("OldPassword123!") });
        userRepository.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var service = CreateService(userRepository.Object);

        var result = await service.ChangePasswordAsync(1, new ChangePasswordRequest
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("successfully");
    }

    private static AuthService CreateService(IUserRepository? userRepository = null)
    {
        userRepository ??= new Mock<IUserRepository>().Object;
        var roleRepository = new Mock<IRoleRepository>().Object;
        var refreshTokenRepository = new Mock<IRefreshTokenRepository>().Object;

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
