using System.Security.Claims;
using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Api.Filters;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
[ServiceFilter(typeof(BindEmployeeIdFromRouteFilter))]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeesController(IEmployeeService service)
    {
        _service = service;
    }

    /// <summary>Danh sách: search / filter / sort / pagination → 200</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? searchName = null,
        [FromQuery] string? searchEmail = null,
        [FromQuery] string? searchPhone = null,
        [FromQuery] int? departmentId = null,
        [FromQuery] int? positionId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortDir = null)
    {
        var result = await _service.GetAllAsync(new EmployeeListQuery
        {
            Page = page,
            PageSize = pageSize,
            Search = search,
            SearchName = searchName,
            SearchEmail = searchEmail,
            SearchPhone = searchPhone,
            DepartmentId = departmentId,
            PositionId = positionId,
            Status = status,
            SortBy = sortBy,
            SortDir = sortDir
        });

        return Ok(result);
    }

    /// <summary>Chi tiết → 200 / 404</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetByIdAsync(id);
        return item == null
            ? NotFound(ApiResponse.Fail("Không tìm thấy nhân viên"))
            : Ok(item);
    }

    /// <summary>Tạo mới → 201</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] EmployeeDto request)
    {
        var created = await _service.CreateAsync(request, GetCurrentUserId());
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Cập nhật → 200 / 404 / 400</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(int id, [FromBody] EmployeeDto request)
    {
        request.Id = id;
        await _service.UpdateAsync(id, request, GetCurrentUserId());
        return Ok(ApiResponse.Ok(message: "Cập nhật nhân viên thành công"));
    }

    /// <summary>Xóa mềm → 200 / 404</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok(message: "Xóa nhân viên thành công"));
    }

    /// <summary>Khôi phục → 200 / 404</summary>
    [HttpPatch("{id:int}/restore")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restore(int id)
    {
        await _service.RestoreAsync(id, GetCurrentUserId());
        return Ok(ApiResponse.Ok(message: "Khôi phục nhân viên thành công"));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }
}
