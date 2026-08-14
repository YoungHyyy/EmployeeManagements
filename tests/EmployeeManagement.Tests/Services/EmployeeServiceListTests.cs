using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EmployeeManagement.Tests.Services;

public class EmployeeServiceListTests
{
    [Fact]
    public async Task GetAllAsync_ShouldReturnPagedResult_WithTotalCount()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(x => x.ListAsync(It.IsAny<EmployeeListQuery>()))
            .ReturnsAsync((
                new List<Employee>
                {
                    new() { Id = 1, FullName = "Nguyen A", Email = "a@mail.com", EmployeeCode = "EMP1", DepartmentId = 1, PositionId = 1, HireDate = DateTime.Today, Status = "Working", IsActive = true },
                    new() { Id = 2, FullName = "Tran B", Email = "b@mail.com", EmployeeCode = "EMP2", DepartmentId = 1, PositionId = 2, HireDate = DateTime.Today, Status = "Working", IsActive = true }
                },
                15));

        var service = new EmployeeService(repo.Object);

        var result = await service.GetAllAsync(new EmployeeListQuery
        {
            Page = 1,
            PageSize = 2,
            Search = "nguyen",
            SortBy = "fullName",
            SortDir = "asc"
        });

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(15);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.TotalPages.Should().Be(8);

        repo.Verify(x => x.ListAsync(It.Is<EmployeeListQuery>(q =>
            q.Search == "nguyen" &&
            q.SortBy == "fullName" &&
            q.SortDir == "asc")), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_ShouldNormalizeInvalidPaging()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(x => x.ListAsync(It.IsAny<EmployeeListQuery>()))
            .ReturnsAsync((Enumerable.Empty<Employee>(), 0));

        var service = new EmployeeService(repo.Object);

        var result = await service.GetAllAsync(new EmployeeListQuery
        {
            Page = 0,
            PageSize = 999
        });

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        repo.Verify(x => x.ListAsync(It.Is<EmployeeListQuery>(q => q.Page == 1 && q.PageSize == 20)), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_ShouldThrow_WhenEmployeeDoesNotExist()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(x => x.ExistsIncludingDeletedAsync(999)).ReturnsAsync(false);

        var service = new EmployeeService(repo.Object);

        var act = async () => await service.RestoreAsync(999);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Không tìm thấy nhân viên");
        repo.Verify(x => x.RestoreAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RestoreAsync_ShouldRestore_WhenSoftDeletedEmployeeExists()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(x => x.ExistsIncludingDeletedAsync(5)).ReturnsAsync(true);
        repo.Setup(x => x.RestoreAsync(5)).Returns(Task.CompletedTask);

        var service = new EmployeeService(repo.Object);

        await service.RestoreAsync(5);

        repo.Verify(x => x.RestoreAsync(5), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldLinkExistingUser_AndValidateDepartment()
    {
        var repo = new Mock<IEmployeeRepository>();
        repo.Setup(x => x.ExistsByEmailAsync("a@mail.com", null)).ReturnsAsync(false);
        repo.Setup(x => x.ExistsByEmployeeCodeAsync(It.IsAny<string>())).ReturnsAsync(false);
        repo.Setup(x => x.GetByUserIdAsync(9)).ReturnsAsync((Employee?)null);
        repo.Setup(x => x.CreateAsync(It.IsAny<Employee>())).ReturnsAsync(3);

        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("a@mail.com")).ReturnsAsync(new User { Id = 9, Email = "a@mail.com" });

        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);
        var positions = new Mock<IPositionRepository>();
        positions.Setup(x => x.ExistsAsync(2)).ReturnsAsync(true);

        var service = new EmployeeService(repo.Object, null, users.Object, departments.Object, positions.Object);

        var created = await service.CreateAsync(new EmployeeDto
        {
            FullName = "Nguyen A",
            Email = "a@mail.com",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today
        });

        created.UserId.Should().Be(9);
        created.EmployeeCode.Should().StartWith("EMP");
        repo.Verify(x => x.CreateAsync(It.Is<Employee>(e => e.UserId == 9)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenDepartmentDoesNotExist()
    {
        var repo = new Mock<IEmployeeRepository>();
        var departments = new Mock<IDepartmentRepository>();
        departments.Setup(x => x.ExistsAsync(99)).ReturnsAsync(false);
        var positions = new Mock<IPositionRepository>();
        positions.Setup(x => x.ExistsAsync(1)).ReturnsAsync(true);

        var service = new EmployeeService(repo.Object, null, null, departments.Object, positions.Object);

        var act = async () => await service.CreateAsync(new EmployeeDto
        {
            FullName = "A",
            Email = "a@mail.com",
            DepartmentId = 99,
            PositionId = 1,
            HireDate = DateTime.Today
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Không tìm thấy phòng ban");
        repo.Verify(x => x.CreateAsync(It.IsAny<Employee>()), Times.Never);
    }
}
