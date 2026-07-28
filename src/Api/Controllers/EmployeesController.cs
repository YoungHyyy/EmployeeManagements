using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAdmin)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _repository;

    public EmployeesController(IEmployeeRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null, [FromQuery] int? departmentId = null, [FromQuery] int? positionId = null, [FromQuery] string? status = null)
    {
        var items = await _repository.ListAsync(page, pageSize, search, departmentId, positionId, status);
        return Ok(items.Select(Map));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(Map(item));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Create([FromBody] EmployeeDto request)
    {
        var code = GenerateEmployeeCode();
        var entity = new Employee
        {
            EmployeeCode = code,
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            DepartmentId = request.DepartmentId,
            PositionId = request.PositionId,
            HireDate = request.HireDate,
            Status = request.Status ?? "Working",
            AvatarUrl = request.AvatarUrl,
            IsActive = request.IsActive
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, Map(entity));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeDto request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.FullName = request.FullName;
        existing.Email = request.Email;
        existing.PhoneNumber = request.PhoneNumber;
        existing.DateOfBirth = request.DateOfBirth;
        existing.Gender = request.Gender;
        existing.Address = request.Address;
        existing.DepartmentId = request.DepartmentId;
        existing.PositionId = request.PositionId;
        existing.HireDate = request.HireDate;
        existing.Status = request.Status ?? existing.Status;
        existing.AvatarUrl = request.AvatarUrl;
        existing.IsActive = request.IsActive;
        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        await _repository.SoftDeleteAsync(id);
        return NoContent();
    }

    [HttpPatch("{id}/restore")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public async Task<IActionResult> Restore(int id)
    {
        await _repository.RestoreAsync(id);
        return NoContent();
    }

    private static EmployeeDto Map(Employee employee) => new()
    {
        Id = employee.Id,
        EmployeeCode = employee.EmployeeCode,
        FullName = employee.FullName,
        Email = employee.Email,
        PhoneNumber = employee.PhoneNumber,
        DateOfBirth = employee.DateOfBirth,
        Gender = employee.Gender,
        Address = employee.Address,
        DepartmentId = employee.DepartmentId,
        PositionId = employee.PositionId,
        HireDate = employee.HireDate,
        Status = employee.Status,
        AvatarUrl = employee.AvatarUrl,
        IsActive = employee.IsActive
    };

    private static string GenerateEmployeeCode()
    {
        var date = DateTime.UtcNow.ToString("yyMMdd");
        var random = new Random().Next(1000, 9999);
        return $"EMP{date}{random}";
    }
}
