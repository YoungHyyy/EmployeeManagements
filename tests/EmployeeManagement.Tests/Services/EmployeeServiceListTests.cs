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
}
