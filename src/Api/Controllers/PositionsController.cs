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
public class PositionsController : ControllerBase
{
    private readonly IPositionRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;

    public PositionsController(IPositionRepository repository, IEmployeeRepository employeeRepository)
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
    public async Task<IActionResult> Create([FromBody] PositionDto request)
    {
        var entity = new Position
        {
            Code = request.Code,
            Name = request.Name,
            Description = request.Description,
            Level = request.Level,
            IsActive = request.IsActive
        };

        var id = await _repository.CreateAsync(entity);
        entity.Id = id;
        return CreatedAtAction(nameof(GetById), new { id }, Map(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] PositionDto request)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Code = request.Code;
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.Level = request.Level;
        existing.IsActive = request.IsActive;
        await _repository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Check if any employees are linked to this position
        var linkedEmployees = await _employeeRepository.ListAsync(1, 1, null, null, id, null);
        if (linkedEmployees.Any())
        {
            return BadRequest(new { message = "Cannot delete position while employees are linked to it" });
        }

        await _repository.SoftDeleteAsync(id);
        return NoContent();
    }

    private static PositionDto Map(Position position) => new()
    {
        Id = position.Id,
        Code = position.Code,
        Name = position.Name,
        Description = position.Description,
        Level = position.Level,
        IsActive = position.IsActive
    };
}
