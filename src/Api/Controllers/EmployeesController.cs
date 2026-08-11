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

    /// <summary>
    /// Danh sách nhân viên: search / filter / sort / pagination.
    /// </summary>
    /// <param name="page">Trang (mặc định 1)</param>
    /// <param name="pageSize">Số bản ghi/trang (mặc định 20, max 100)</param>
    /// <param name="search">Tìm theo họ tên HOẶC email HOẶC SĐT</param>
    /// <param name="searchName">Tìm theo họ tên</param>
    /// <param name="searchEmail">Tìm theo email</param>
    /// <param name="searchPhone">Tìm theo số điện thoại</param>
    /// <param name="departmentId">Lọc phòng ban</param>
    /// <param name="positionId">Lọc chức vụ</param>
    /// <param name="status">Lọc trạng thái (Working, Probation, Resigned, OnLeave)</param>
    /// <param name="sortBy">fullName | createdAt | hireDate</param>
    /// <param name="sortDir">asc | desc</param>
    [HttpGet]
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
        // Id từ route (filter cũng gán trước validation)
        request.Id = id;
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
