using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EmployeeManagement.Tests.Services;

public class EmployeeAccountServiceTests
{
    [Fact]
    public async Task ProvisionAsync_ShouldCreateUserAndEmployee_WithMatchingUserId()
    {
        var userRepo = new Mock<IUserRepository>();
        var employeeRepo = new Mock<IEmployeeRepository>();
        var roleRepo = new Mock<IRoleRepository>();
        var deptRepo = new Mock<IDepartmentRepository>();
        var posRepo = new Mock<IPositionRepository>();

        userRepo.Setup(x => x.GetByEmailAsync("nv.a@example.com")).ReturnsAsync((User?)null);
        employeeRepo.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        deptRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Department { Id = 1, Code = "IT", Name = "IT" });
        posRepo.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Position { Id = 2, Code = "DEV", Name = "Dev" });
        roleRepo.Setup(x => x.GetRoleIdByCodeAsync("EMPLOYEE")).ReturnsAsync(3);
        userRepo.Setup(x => x.CreateWithRoleAndEmployeeAsync(It.IsAny<User>(), 3, It.IsAny<Employee>()))
            .ReturnsAsync((User _, int _, Employee employee) =>
            {
                employee.UserId = 10;
                employee.Id = 20;
                return (10, 20);
            });

        var service = new EmployeeAccountService(
            userRepo.Object, employeeRepo.Object, roleRepo.Object, deptRepo.Object, posRepo.Object);

        var result = await service.ProvisionAsync(new ProvisionEmployeeAccountRequest
        {
            FullName = "Nguyen Van A",
            Email = "nv.a@example.com",
            Password = "Password123",
            PhoneNumber = "0901234567",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today
        });

        result.UserId.Should().Be(10);
        result.Employee.Id.Should().Be(20);
        result.Employee.UserId.Should().Be(10);
        result.Employee.AvatarUrl.Should().BeNull();
        userRepo.Verify(x => x.CreateWithRoleAndEmployeeAsync(
            It.Is<User>(u => u.Email == "nv.a@example.com" && !string.IsNullOrEmpty(u.PasswordHash)),
            3,
            It.Is<Employee>(e => e.Email == "nv.a@example.com" && e.AvatarUrl == null)), Times.Once);
        userRepo.Verify(x => x.CreateWithRoleAsync(It.IsAny<User>(), It.IsAny<int>()), Times.Never);
        userRepo.Verify(x => x.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_ShouldThrow_WhenEmailAlreadyExistsOnUser()
    {
        var userRepo = new Mock<IUserRepository>();
        userRepo.Setup(x => x.GetByEmailAsync("dup@example.com"))
            .ReturnsAsync(new User { Id = 1, Email = "dup@example.com" });

        var service = new EmployeeAccountService(
            userRepo.Object,
            Mock.Of<IEmployeeRepository>(),
            Mock.Of<IRoleRepository>(),
            Mock.Of<IDepartmentRepository>(),
            Mock.Of<IPositionRepository>());

        var act = async () => await service.ProvisionAsync(new ProvisionEmployeeAccountRequest
        {
            FullName = "Dup",
            Email = "dup@example.com",
            Password = "Password123",
            DepartmentId = 1,
            PositionId = 1
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Email đã tồn tại*");
        userRepo.Verify(x => x.CreateWithRoleAndEmployeeAsync(
            It.IsAny<User>(), It.IsAny<int>(), It.IsAny<Employee>()), Times.Never);
    }

    [Fact]
    public async Task ProvisionAsync_ShouldThrow_WhenDepartmentMissing()
    {
        var userRepo = new Mock<IUserRepository>();
        var employeeRepo = new Mock<IEmployeeRepository>();
        var deptRepo = new Mock<IDepartmentRepository>();
        userRepo.Setup(x => x.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        employeeRepo.Setup(x => x.ExistsByEmailAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);
        deptRepo.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((Department?)null);

        var service = new EmployeeAccountService(
            userRepo.Object,
            employeeRepo.Object,
            Mock.Of<IRoleRepository>(),
            deptRepo.Object,
            Mock.Of<IPositionRepository>());

        var act = async () => await service.ProvisionAsync(new ProvisionEmployeeAccountRequest
        {
            FullName = "A",
            Email = "a@example.com",
            Password = "Password123",
            DepartmentId = 99,
            PositionId = 1
        });

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*phòng ban*");
    }
}
