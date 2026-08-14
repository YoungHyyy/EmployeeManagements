using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EmployeeManagement.Tests.Services;

public class ProfileServiceTests
{
    [Fact]
    public async Task GetByEmailAsync_ShouldIncludeEmployeeFields_WhenLinked()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("a@mail.com")).ReturnsAsync(new User
        {
            Id = 4,
            Email = "a@mail.com",
            FullName = "Nguyen A",
            PhoneNumber = "0901234567",
            CreatedAt = DateTime.UtcNow
        });

        var employees = new Mock<IEmployeeRepository>();
        employees.Setup(x => x.GetByUserIdAsync(4)).ReturnsAsync(new Employee
        {
            Id = 20,
            UserId = 4,
            EmployeeCode = "EMP001",
            DepartmentId = 1,
            PositionId = 2,
            DateOfBirth = new DateTime(1995, 1, 2),
            Gender = "Male",
            Address = "HN",
            HireDate = DateTime.Today,
            Status = "Working"
        });

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(new Department { Id = 1, Name = "IT" });
        var positions = new Mock<IPositionRepository>();
        positions.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(new Position { Id = 2, Name = "Dev" });

        var service = new ProfileService(
            users.Object,
            Mock.Of<IAvatarUploadService>(),
            employees.Object,
            departments.Object,
            positions.Object);

        var profile = await service.GetByEmailAsync("a@mail.com");

        profile.Should().NotBeNull();
        profile!.EmployeeId.Should().Be(20);
        profile.EmployeeCode.Should().Be("EMP001");
        profile.Gender.Should().Be("Male");
        profile.Address.Should().Be("HN");
        profile.DepartmentName.Should().Be("IT");
        profile.PositionName.Should().Be("Dev");
    }

    [Fact]
    public async Task UpdateByEmailAsync_ShouldWriteEmployeeHrFields()
    {
        var user = new User { Id = 4, Email = "a@mail.com", FullName = "Old", PhoneNumber = "0901111111" };
        var employee = new Employee { Id = 20, UserId = 4, FullName = "Old", DepartmentId = 1, PositionId = 1 };

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("a@mail.com")).ReturnsAsync(user);
        users.Setup(x => x.UpdateAsync(It.IsAny<User>())).Returns(Task.CompletedTask);

        var employees = new Mock<IEmployeeRepository>();
        employees.Setup(x => x.GetByUserIdAsync(4)).ReturnsAsync(employee);
        employees.Setup(x => x.UpdateAsync(It.IsAny<Employee>())).Returns(Task.CompletedTask);

        var service = new ProfileService(users.Object, Mock.Of<IAvatarUploadService>(), employees.Object);

        await service.UpdateByEmailAsync("a@mail.com", new ProfileUpdateRequest
        {
            FullName = "Nguyen A",
            PhoneNumber = "0901234567",
            Gender = "Female",
            Address = "HCM",
            DateOfBirth = new DateTime(1998, 5, 5)
        });

        employee.FullName.Should().Be("Nguyen A");
        employee.Gender.Should().Be("Female");
        employee.Address.Should().Be("HCM");
        employee.DateOfBirth.Should().Be(new DateTime(1998, 5, 5));
        employees.Verify(x => x.UpdateAsync(employee), Times.Once);
    }
}
