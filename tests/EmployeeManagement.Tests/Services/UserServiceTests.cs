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
    public async Task CreateAsync_ShouldThrow_WhenTryingToCreateAdminByNonAdmin()
    {
        var userRepo = new Mock<IUserRepository>();
        var roleRepo = new Mock<IRoleRepository>();

        var service = new UserService(userRepo.Object, roleRepo.Object);

        var act = async () => await service.CreateAsync(new CreateUserRequest
        {
            FullName = "Test",
            Email = "test@example.com",
            Password = "Password123!",
            RoleCode = "ADMIN"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Chỉ Admin được phép tạo tài khoản Admin");
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
        var service = new UserService(userRepo.Object, Mock.Of<IRoleRepository>());

        await service.DeleteAsync(10);

        userRepo.Verify(x => x.SoftDeleteAsync(10), Times.Once);
    }
}
