using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public DepartmentsController(IDepartmentRepository repository, IEmployeeRepository employeeRepository)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var items = await _repository.ListAsync(page, pageSize);
        return Ok(items.Select(x => Map(x)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _repository.GetByIdAsync(id);
        return item == null ? NotFound() : Ok(Map(item));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DepartmentDto request)
    {
        var entity = new Department
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            IsActive = request.IsActive
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, Map(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] DepartmentDto request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.IsActive = request.IsActive;
        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Check if any employees are linked to this department
        var linkedEmployees = await _employeeRepository.ListAsync(1, 1, null, id, null, null);
        if (linkedEmployees.Any())
        {
            return BadRequest(new { message = "Không thể xóa phòng ban khi vẫn còn nhân viên liên kết" });
        }

        await _repository.SoftDeleteAsync(id);
        return NoContent();
    }

    private static DepartmentDto Map(Department department) => new()
    {
        Id = department.Id,
        Code = department.Code,
        Name = department.Name,
        Description = department.Description,
        IsActive = department.IsActive
    };
}
