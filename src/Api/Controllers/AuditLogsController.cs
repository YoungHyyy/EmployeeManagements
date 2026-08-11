using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// Admin-only audit trail. Read-only (append-only store).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// GET /api/AuditLogs?page=1&amp;pageSize=20&amp;userId=&amp;action=&amp;module=&amp;from=&amp;to=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? module = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _auditLogService.GetListAsync(new AuditLogQuery
        {
            Page = page,
            PageSize = pageSize,
            UserId = userId,
            Action = action,
            Module = module,
            From = from,
            To = to
        });

        return Ok(result);
    }
}
