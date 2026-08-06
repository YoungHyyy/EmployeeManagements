using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Validators;
using EmployeeManagement.Domain.Entities;
using FluentAssertions;

namespace EmployeeManagement.Tests.Validators;

public class EmployeeDtoValidatorTests
{
    private readonly EmployeeDtoValidator _validator = new(new FakeEmployeeRepository());

    [Fact]
    public async Task Should_fail_when_required_fields_are_missing()
    {
        var request = new EmployeeDto
        {
            FullName = "",
            Email = "not-an-email",
            DepartmentId = 0,
            PositionId = 0,
            HireDate = DateTime.Today.AddDays(-1)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "FullName");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
        result.Errors.Should().Contain(e => e.PropertyName == "DepartmentId");
        result.Errors.Should().Contain(e => e.PropertyName == "PositionId");
    }

    [Fact]
    public async Task Should_pass_when_employee_request_is_valid()
    {
        var request = new EmployeeDto
        {
            FullName = "Nguyen Van A",
            Email = "a.nguyen@example.com",
            PhoneNumber = "0901234567",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today.AddDays(-30),
            Gender = "Male",
            Status = "Working"
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_fail_when_email_already_exists()
    {
        var validator = new EmployeeDtoValidator(new FakeEmployeeRepository());
        var request = new EmployeeDto
        {
            FullName = "Nguyen Van B",
            Email = "existing@example.com",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today.AddDays(-30)
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    private sealed class FakeEmployeeRepository : IEmployeeRepository
    {
        public Task<int> CreateAsync(Employee employee) => Task.FromResult(0);
        public Task UpdateAsync(Employee employee) => Task.CompletedTask;
        public Task SoftDeleteAsync(int id) => Task.CompletedTask;
        public Task RestoreAsync(int id) => Task.CompletedTask;
        public Task<Employee?> GetByIdAsync(int id) => Task.FromResult<Employee?>(null);
        public Task<IEnumerable<Employee>> ListAsync(int page, int pageSize, string? search = null, int? departmentId = null, int? positionId = null, string? status = null) => Task.FromResult<IEnumerable<Employee>>(Array.Empty<Employee>());
        public Task<bool> ExistsByEmailAsync(string email, int? excludeEmployeeId = null) => Task.FromResult(string.Equals(email, "existing@example.com", StringComparison.OrdinalIgnoreCase));
    }
}
