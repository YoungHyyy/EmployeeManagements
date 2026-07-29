using System.Threading.Tasks;
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
        private readonly IUserRepository _userRepository;

        public UsersController(IUserService userService, IUserRepository userRepository)
        {
            _userService = userService;
            _userRepository = userRepository;
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
            return u == null ? NotFound() : Ok(u);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest req)
        {
            var existing = await _userRepository.GetByEmailAsync(req.Email);
            if (existing != null)
            {
                return BadRequest(new { message = "Email đã tồn tại" });
            }

            var created = await _userService.CreateAsync(req);
            return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest req)
        {
            await _userService.UpdateAsync(id, req);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Prevent deleting own account
            var currentIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrWhiteSpace(currentIdClaim) && int.TryParse(currentIdClaim, out var currentId))
            {
                if (currentId == id)
                {
                    return BadRequest(new { message = "Không thể xóa tài khoản của chính mình" });
                }
            }
            else
            {
                // Fallback: attempt to resolve by email
                var email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name;
                if (!string.IsNullOrWhiteSpace(email))
                {
                    var currentUser = await _userRepository.GetByEmailAsync(email);
                    if (currentUser != null && currentUser.Id == id)
                    {
                        return BadRequest(new { message = "Cannot delete your own account" });
                    }
                }
            }

            await _userService.DeleteAsync(id);
            return NoContent();
        }
    }
}
