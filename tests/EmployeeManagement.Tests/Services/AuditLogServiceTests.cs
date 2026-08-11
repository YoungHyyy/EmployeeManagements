using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Services;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;
using Moq;

namespace EmployeeManagement.Tests.Services;

public class AuditLogServiceTests
{
    [Fact]
    public async Task WriteAsync_ShouldCallRepository_WhenRequestIsValid()
    {
        var repo = new Mock<IAuditLogRepository>();
        repo.Setup(x => x.AddAsync(It.IsAny<AuditLog>())).Returns(Task.CompletedTask);

        var service = new AuditLogService(repo.Object);

        await service.WriteAsync(new AuditLogWriteRequest
        {
            UserId = 1,
            Action = "LOGIN_SUCCESS",
            Module = "Auth",
            EntityName = "User",
            EntityId = "1"
        });

        repo.Verify(x => x.AddAsync(It.Is<AuditLog>(l =>
            l.UserId == 1 &&
            l.Action == "LOGIN_SUCCESS" &&
            l.Module == "Auth")), Times.Once);
    }

    [Fact]
    public async Task WriteAsync_ShouldNotThrow_WhenRepositoryFails()
    {
        var repo = new Mock<IAuditLogRepository>();
        repo.Setup(x => x.AddAsync(It.IsAny<AuditLog>()))
            .ThrowsAsync(new Exception("DB down"));

        var service = new AuditLogService(repo.Object);

        var act = async () => await service.WriteAsync(new AuditLogWriteRequest
        {
            Action = "LOGIN_FAILED",
            Module = "Auth"
        });

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task WriteAsync_ShouldSkip_WhenActionIsEmpty()
    {
        var repo = new Mock<IAuditLogRepository>();
        var service = new AuditLogService(repo.Object);

        await service.WriteAsync(new AuditLogWriteRequest { Action = "  ", Module = "Auth" });

        repo.Verify(x => x.AddAsync(It.IsAny<AuditLog>()), Times.Never);
    }

    [Fact]
    public async Task GetListAsync_ShouldMapItemsAndNormalizePaging()
    {
        var repo = new Mock<IAuditLogRepository>();
        repo.Setup(x => x.ListAsync(It.IsAny<AuditLogQuery>()))
            .ReturnsAsync((
                new List<AuditLog>
                {
                    new()
                    {
                        Id = 9,
                        UserId = 1,
                        Action = "CREATE",
                        Module = "Employee",
                        EntityId = "3",
                        CreatedAt = DateTime.UtcNow
                    }
                },
                1));

        var service = new AuditLogService(repo.Object);

        var result = await service.GetListAsync(new AuditLogQuery { Page = 0, PageSize = 999 });

        result.Page.Should().Be(1);
        result.PageSize.Should().Be(20);
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items[0].Action.Should().Be("CREATE");
        result.Items[0].Module.Should().Be("Employee");
    }
}
