using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EmployeeManagement.Tests.Services;

public class UserServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateAdmin_WhenRoleCodeIsAdmin()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        userRepo.Setup(x => x.CreateWithRoleAsync(It.IsAny<User>(), 1)).ReturnsAsync(10);
        userRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new User
        {
            Id = 10,
            Email = "admin.new@example.com",
            FullName = "New Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(x => x.GetRoleIdByCodeAsync("ADMIN")).ReturnsAsync(1);

        var service = new UserService(userRepo.Object, roleRepo.Object, Mock.Of<IAuditLogService>());

        var result = await service.CreateAsync(new CreateUserRequest
        {
            FullName = "New Admin",
            Email = "admin.new@example.com",
            Password = "Password123!",
            RoleCode = "ADMIN"
        });

        result.Id.Should().Be(10);
        result.Email.Should().Be("admin.new@example.com");
        roleRepo.Verify(x => x.GetRoleIdByCodeAsync("ADMIN"), Times.Once);
        userRepo.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), 1), Times.Once);
        userRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        roleRepo.Verify(x => x.AssignRoleAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenRoleCodeIsInvalid()
    {
        var userRepo = new Mock<IUserRepository>();
        var service = new UserService(userRepo.Object, Mock.Of<IRoleRepository>());

        var act = async () => await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Test",
            Email = "test@example.com",
            Password = "Password123!",
            RoleCode = "SUPERADMIN"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Role không hợp lệ*");
        userRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        userRepo.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_ShouldNotPersistUser_WhenRoleIsMissing()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        userRepo.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), null)).ReturnsAsync(false);

        var roleRepo = new Mock<IRoleRepository>();
        roleRepo.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync((int?)null);

        var service = new UserService(userRepo.Object, roleRepo.Object);

        var act = async () => await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Test",
            Email = "test@example.com",
            Password = "Password123!",
            RoleCode = "EMPLOYEE"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không tìm thấy role EMPLOYEE*");
        userRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
        userRepo.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((User?)null);

        var service = new UserService(userRepo.Object, Mock.Of<IRoleRepository>());

        var result = await service.GetByIdAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallSoftDelete()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, Email = "u@mail.com" });
        var service = new UserService(userRepo.Object, Mock.Of<IRoleRepository>());

        await service.DeleteAsync(10);

        userRepo.Verify(x => x.SoftDeleteAsync(10), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRevokeRefreshTokens_WhenDisablingUser()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new User
        {
            Id = 10,
            Email = "u@mail.com",
            FullName = "User",
            IsActive = true
        });
        userRepo.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        refreshTokenRepo.Setup(x => x.RevokeAllUserTokensAsync(10)).Returns(Task.CompletedTask);

        var service = new UserService(
            userRepo.Object,
            Mock.Of<IRoleRepository>(),
            Mock.Of<IAuditLogService>(),
            refreshTokenRepo.Object);

        await service.UpdateAsync(10, new UpdateUserRequest
        {
            FullName = "User",
            IsActive = false
        });

        refreshTokenRepo.Verify(x => x.RevokeAllUserTokensAsync(10), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldNotRevokeRefreshTokens_WhenUserStaysActive()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new User
        {
            Id = 10,
            Email = "u@mail.com",
            FullName = "User",
            IsActive = true
        });
        userRepo.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var refreshTokenRepo = new Mock<IRefreshTokenRepository>();
        var service = new UserService(
            userRepo.Object,
            Mock.Of<IRoleRepository>(),
            Mock.Of<IAuditLogService>(),
            refreshTokenRepo.Object);

        await service.UpdateAsync(10, new UpdateUserRequest
        {
            FullName = "User Updated",
            IsActive = true
        });

        refreshTokenRepo.Verify(x => x.RevokeAllUserTokensAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrow_WhenDeletingOwnAccount()
    {
        var service = new UserService(Mock.Of<IUserRepository>(), Mock.Of<IRoleRepository>());

        var act = async () => await service.DeleteAsync(5, currentUserId: 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không thể xóa tài khoản của chính mình*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRevokeRefreshTokens()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new User { Id = 10, Email = "u@mail.com" });
        var refresh = new Mock<IRefreshTokenRepository>();

        var service = new UserService(
            userRepo.Object,
            Mock.Of<IRoleRepository>(),
            Mock.Of<IAuditLogService>(),
            refresh.Object);

        await service.DeleteAsync(10, currentUserId: 1);

        refresh.Verify(x => x.RevokeAllUserTokensAsync(10), Times.Once);
    }
}
