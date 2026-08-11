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

    [Fact]
    public async Task Should_pass_when_update_keeps_same_email_with_exclude_id()
    {
        // Employee Id=5 already owns existing@example.com — update must allow same email
        var validator = new EmployeeDtoValidator(new FakeEmployeeRepository(ownedEmailById: (5, "existing@example.com")));
        var request = new EmployeeDto
        {
            Id = 5,
            FullName = "Nguyen Van B Updated",
            Email = "existing@example.com",
            PhoneNumber = "0901234567",
            DepartmentId = 1,
            PositionId = 2,
            HireDate = DateTime.Today.AddDays(-30),
            Gender = "Male",
            Status = "Working"
        };

        var result = await validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Should_fail_when_update_uses_another_employees_email()
    {
        var validator = new EmployeeDtoValidator(new FakeEmployeeRepository(ownedEmailById: (5, "existing@example.com")));
        var request = new EmployeeDto
        {
            Id = 99, // different employee
            FullName = "Other",
            Email = "existing@example.com",
            PhoneNumber = "0901234567",
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
        private readonly (int Id, string Email)? _ownedEmailById;

        public FakeEmployeeRepository((int Id, string Email)? ownedEmailById = null)
        {
            _ownedEmailById = ownedEmailById ?? (0, "existing@example.com");
        }

        public Task<int> CreateAsync(Employee employee) => Task.FromResult(0);
        public Task UpdateAsync(Employee employee) => Task.CompletedTask;
        public Task SoftDeleteAsync(int id) => Task.CompletedTask;
        public Task RestoreAsync(int id) => Task.CompletedTask;
        public Task<Employee?> GetByIdAsync(int id) => Task.FromResult<Employee?>(null);
        public Task<(IEnumerable<Employee> Items, int TotalCount)> ListAsync(EmployeeListQuery query)
            => Task.FromResult<(IEnumerable<Employee>, int)>((Array.Empty<Employee>(), 0));

        public Task<bool> ExistsByEmailAsync(string email, int? excludeEmployeeId = null)
        {
            var (ownerId, ownedEmail) = _ownedEmailById!.Value;
            if (!string.Equals(email, ownedEmail, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(false);
            }

            // Email exists, unless we are excluding the owner
            if (excludeEmployeeId.HasValue && excludeEmployeeId.Value == ownerId)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
