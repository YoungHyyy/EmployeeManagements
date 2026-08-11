using System.Security.Claims;
using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AuthorizationPolicies.AdminOnly)]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var list = await _userService.ListAsync(page, pageSize);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var u = await _userService.GetByIdAsync(id);
            return u == null
                ? NotFound(new { success = false, message = "Không tìm thấy người dùng" })
                : Ok(u);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            var created = await _userService.CreateAsync(req, GetCurrentUserId());
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
        {
            await _userService.UpdateAsync(id, req, GetCurrentUserId());
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id, GetCurrentUserId());
            return NoContent();
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
