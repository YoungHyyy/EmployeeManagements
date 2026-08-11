using System.Security.Claims;
using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] int? positionId = null,
        [FromQuery] string? status = null)
    {
        var items = await _service.GetAllAsync(page, pageSize, search, departmentId, positionId, status);
        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null
            ? NotFound(new { success = false, message = "Không tìm thấy nhân viên" })
            : Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] EmployeeDto request)
    {
        var created = await _service.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeDto request)
    {
        await _service.UpdateAsync(id, request, GetCurrentUserId());
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetCurrentUserId());
        return NoContent();
    }

    [HttpPatch("{id}/restore")]
    public async Task<IActionResult> Restore(int id)
    {
        await _service.RestoreAsync(id, GetCurrentUserId());
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
