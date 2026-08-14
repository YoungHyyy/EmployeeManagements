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
    public async Task GetByUserIdAsync_ShouldReturnEmployeeLinkedToJwtUser_AndAvatarFromUser()
    {
        var userRepo = new Mock<IUserRepository>();
        var employeeRepo = new Mock<IEmployeeRepository>();
        userRepo.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new User
        {
            Id = 7,
            Email = "me@example.com",
            FullName = "Me",
            AvatarUrl = "/uploads/avatars/me.png",
            CreatedAt = DateTime.UtcNow
        });
        employeeRepo.Setup(x => x.GetByUserIdAsync(7)).ReturnsAsync(new Employee
        {
            Id = 99,
            UserId = 7,
            EmployeeCode = "EMP1",
            FullName = "Me",
            Email = "me@example.com",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today,
            Status = "Working",
            AvatarUrl = "/should-not-use.png"
        });

        var service = new ProfileService(userRepo.Object, employeeRepo.Object, Mock.Of<IAvatarUploadService>());

        var profile = await service.GetByUserIdAsync(7);

        profile.Should().NotBeNull();
        profile!.Id.Should().Be(7);
        profile.EmployeeId.Should().Be(99);
        profile.AvatarUrl.Should().Be("/uploads/avatars/me.png");
        employeeRepo.Verify(x => x.GetByUserIdAsync(7), Times.Once);
        employeeRepo.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserProfile_WhenAdminHasNoEmployee()
    {
        var userRepo = new Mock<IUserRepository>();
        var employeeRepo = new Mock<IEmployeeRepository>();
        userRepo.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(new User
        {
            Id = 7,
            Email = "admin@example.com",
            FullName = "Admin",
            AvatarUrl = "/uploads/avatars/admin.png"
        });
        employeeRepo.Setup(x => x.GetByUserIdAsync(7)).ReturnsAsync((Employee?)null);

        var service = new ProfileService(userRepo.Object, employeeRepo.Object, Mock.Of<IAvatarUploadService>());

        var profile = await service.GetByUserIdAsync(7);

        profile.Should().NotBeNull();
        profile!.Id.Should().Be(7);
        profile.EmployeeId.Should().Be(0);
        profile.FullName.Should().Be("Admin");
        profile.AvatarUrl.Should().Be("/uploads/avatars/admin.png");
    }

    [Fact]
    public async Task UpdateByUserIdAsync_ShouldWriteEmployeeHrFields_AndNotTouchEmployeeAvatar()
    {
        var userRepo = new Mock<IUserRepository>();
        var employeeRepo = new Mock<IEmployeeRepository>();
        var user = new User { Id = 7, Email = "me@example.com", FullName = "Old", AvatarUrl = "/a.png" };
        var employee = new Employee
        {
            Id = 99,
            UserId = 7,
            FullName = "Old",
            Email = "me@example.com",
            AvatarUrl = "/old-emp.png",
            DepartmentId = 1,
            PositionId = 1,
            HireDate = DateTime.Today
        };
        userRepo.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(user);
        employeeRepo.Setup(x => x.GetByUserIdAsync(7)).ReturnsAsync(employee);

        var service = new ProfileService(userRepo.Object, employeeRepo.Object, Mock.Of<IAvatarUploadService>());

        await service.UpdateByUserIdAsync(7, new ProfileUpdateRequest
        {
            FullName = "New Name",
            PhoneNumber = "0901234567",
            DateOfBirth = new DateTime(1995, 1, 2),
            Gender = "Female",
            Address = "Hanoi"
        });

        employee.FullName.Should().Be("New Name");
        employee.DateOfBirth.Should().Be(new DateTime(1995, 1, 2));
        employee.Gender.Should().Be("Female");
        employee.Address.Should().Be("Hanoi");
        employee.AvatarUrl.Should().Be("/old-emp.png");
        user.FullName.Should().Be("New Name");
        user.AvatarUrl.Should().Be("/a.png");
        employeeRepo.Verify(x => x.UpdateAsync(employee), Times.Once);
        userRepo.Verify(x => x.UpdateAsync(user), Times.Once);
        employeeRepo.Verify(x => x.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task UploadAvatarByUserIdAsync_ShouldWriteUsersAvatarOnly()
    {
        var userRepo = new Mock<IUserRepository>();
        var employeeRepo = new Mock<IEmployeeRepository>();
        var avatar = new Mock<IAvatarUploadService>();
        var user = new User { Id = 7, Email = "me@example.com" };
        var employee = new Employee { Id = 99, UserId = 7, AvatarUrl = "/emp.png" };
        userRepo.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(user);
        employeeRepo.Setup(x => x.GetByUserIdAsync(7)).ReturnsAsync(employee);
        avatar.Setup(x => x.SaveAsync(It.IsAny<Stream>(), "a.png", "image/png", 10, It.IsAny<string>()))
            .ReturnsAsync("/uploads/avatars/a.png");

        var service = new ProfileService(userRepo.Object, employeeRepo.Object, avatar.Object);

        var path = await service.UploadAvatarByUserIdAsync(
            7, new MemoryStream(new byte[10]), "a.png", "image/png", 10, "/tmp");

        path.Should().Be("/uploads/avatars/a.png");
        user.AvatarUrl.Should().Be("/uploads/avatars/a.png");
        employee.AvatarUrl.Should().Be("/emp.png");
        userRepo.Verify(x => x.UpdateAsync(user), Times.Once);
        employeeRepo.Verify(x => x.UpdateAsync(It.IsAny<Employee>()), Times.Never);
    }
}
