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
}
