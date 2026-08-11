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
        userRepo.Setup(x => x.CreateAsync(It.IsAny<User>())).ReturnsAsync(10);
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
        roleRepo.Setup(x => x.AssignRoleAsync(10, 1)).Returns(Task.CompletedTask);

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
        roleRepo.Verify(x => x.AssignRoleAsync(10, 1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenRoleCodeIsInvalid()
    {
        var service = new UserService(Mock.Of<IUserRepository>(), Mock.Of<IRoleRepository>());

        var act = async () => await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Test",
            Email = "test@example.com",
            Password = "Password123!",
            RoleCode = "SUPERADMIN"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Role không hợp lệ*");
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
    public async Task DeleteAsync_ShouldThrow_WhenDeletingOwnAccount()
    {
        var service = new UserService(Mock.Of<IUserRepository>(), Mock.Of<IRoleRepository>());

        var act = async () => await service.DeleteAsync(5, currentUserId: 5);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Không thể xóa tài khoản của chính mình*");
    }
}
