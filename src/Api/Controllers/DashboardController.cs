using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.AdminOnly)]
public class DashboardController : ControllerBase
{
    private readonly IDashboardRepository _repository;

    public DashboardController(IDashboardRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var result = await _repository.GetStatsAsync();
        return Ok(result);
    }
}
