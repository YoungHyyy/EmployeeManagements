using System;
using System.Threading.Tasks;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace EmployeeManagement.Tests.Authentication
{
    public class AuthServiceTests
    {
        private readonly Mock<IUserRepository> _userRepoMock;
        private readonly Mock<IRoleRepository> _roleRepoMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepoMock;
        private readonly Mock<ITokenService> _tokenServiceMock;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            _userRepoMock = new Mock<IUserRepository>();
            _roleRepoMock = new Mock<IRoleRepository>();
            _refreshTokenRepoMock = new Mock<IRefreshTokenRepository>();
            _tokenServiceMock = new Mock<ITokenService>();
            _tokenServiceMock.Setup(x => x.GenerateAccessToken(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>())).Returns("token");

            _authService = new AuthService(
                _userRepoMock.Object,
                _roleRepoMock.Object,
                _refreshTokenRepoMock.Object,
                _tokenServiceMock.Object
            );
        }

        [Fact]
        public async Task RefreshTokenAsync_EmptyToken_ReturnsFailure()
        {
            // Act
            var result = await _authService.RefreshTokenAsync("");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Refresh token không hợp lệ");
        }

        [Fact]
        public async Task RefreshTokenAsync_TokenNotFound_ReturnsFailure()
        {
            // Arrange
            _refreshTokenRepoMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
                .ReturnsAsync((RefreshToken?)null);

            // Act
            var result = await _authService.RefreshTokenAsync("non_existent_token");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Refresh token không hợp lệ");
        }

        [Fact]
        public async Task RefreshTokenAsync_RevokedToken_ReturnsFailure()
        {
            // Arrange
            var revokedToken = new RefreshToken
            {
                Id = 1,
                UserId = 10,
                TokenHash = "some_hash",
                IsRevoked = true,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };

            _refreshTokenRepoMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
                .ReturnsAsync(revokedToken);

            // Act
            var result = await _authService.RefreshTokenAsync("revoked_token");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Refresh token không hợp lệ");
        }

        [Fact]
        public async Task RefreshTokenAsync_ExpiredToken_ReturnsFailure()
        {
            // Arrange
            var expiredToken = new RefreshToken
            {
                Id = 1,
                UserId = 10,
                TokenHash = "some_hash",
                IsRevoked = false,
                ExpiresAt = DateTime.UtcNow.AddHours(-1)
            };

            _refreshTokenRepoMock.Setup(r => r.GetByTokenHashAsync(It.IsAny<string>()))
                .ReturnsAsync(expiredToken);

            // Act
            var result = await _authService.RefreshTokenAsync("expired_token");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Refresh token không hợp lệ");
        }

        [Fact]
        public async Task LogoutAsync_CallsRevoke_AndReturnsSuccess()
        {
            // Arrange
            _refreshTokenRepoMock.Setup(r => r.RevokeAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _authService.LogoutAsync("valid_token_to_logout");

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Message.Should().Be("Đăng xuất thành công");
            _refreshTokenRepoMock.Verify(r => r.RevokeAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_PasswordMismatch_ReturnsFailure()
        {
            // Arrange
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "OldPassword123!",
                NewPassword = "NewPassword123!",
                ConfirmPassword = "DifferentPassword123!"
            };

            // Act
            var result = await _authService.ChangePasswordAsync(1, request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Be("Mật khẩu mới và xác nhận mật khẩu không khớp");
        }
    }
}
