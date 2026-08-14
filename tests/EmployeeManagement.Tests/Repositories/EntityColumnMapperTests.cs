using EmployeeManagement.Domain.Entities;
using EmployeeManagement.Infrastructure.Repositories;
using FluentAssertions;

namespace EmployeeManagement.Tests.Repositories;

/// <summary>
/// Khóa thiết kế: Generic/Base repo map cột bằng Reflection (không EF).
/// </summary>
public class EntityColumnMapperTests
{
    [Fact]
    public void Employee_Insert_Includes_Business_Columns_Excludes_System()
    {
        var map = EntityColumnMapper.GetMap(
            typeof(Employee),
            EntityColumnMapper.SoftDeleteSystemColumns);

        map.InsertColumns.Should().Contain(new[]
        {
            "DepartmentId", "PositionId", "EmployeeCode", "FullName", "Email", "UserId"
        });
        map.InsertColumns.Should().NotContain("AvatarUrl");
        map.UpdateColumns.Should().NotContain("AvatarUrl");

        // Hệ thống: base tự gán — không nằm INSERT body
        map.InsertColumns.Should().NotContain(new[]
        {
            "Id", "IsDeleted", "CreatedAt", "UpdatedAt", "DeletedAt"
        });
    }

    [Fact]
    public void Employee_Update_Ignores_EmployeeCode_And_UserId()
    {
        var map = EntityColumnMapper.GetMap(
            typeof(Employee),
            EntityColumnMapper.SoftDeleteSystemColumns);

        map.UpdateColumns.Should().Contain("FullName");
        map.UpdateColumns.Should().Contain("Email");
        map.UpdateColumns.Should().NotContain("EmployeeCode"); // [DbUpdateIgnore]
        map.UpdateColumns.Should().NotContain("UserId");      // [DbUpdateIgnore]
        map.UpdateColumns.Should().NotContain("Id");
    }

    [Fact]
    public void User_Insert_Ignores_UserName()
    {
        var map = EntityColumnMapper.GetMap(
            typeof(User),
            EntityColumnMapper.SoftDeleteSystemColumns);

        map.InsertColumns.Should().Contain("Email");
        map.InsertColumns.Should().Contain("PasswordHash");
        map.InsertColumns.Should().NotContain("UserName"); // [DbInsertIgnore]
    }

    [Fact]
    public void AuditLog_Insert_Only_Excludes_Id()
    {
        var map = EntityColumnMapper.GetMap(
            typeof(AuditLog),
            EntityColumnMapper.IdentityOnlySystemColumns);

        map.InsertColumns.Should().Contain(new[]
        {
            "UserId", "Action", "Module", "Details", "CreatedAt"
        });
        map.InsertColumns.Should().NotContain("Id");
    }
}
