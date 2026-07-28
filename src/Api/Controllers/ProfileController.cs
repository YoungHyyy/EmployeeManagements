using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAdmin)]
public class ProfileController : ControllerBase
{
    private readonly IUserRepository _userRepository;

    public ProfileController(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var email = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByEmailAsync(email);
        return user == null ? NotFound() : Ok(new
        {
            user.Id,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.CreatedAt
        });
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
    {
        var email = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return NotFound();

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        await _userRepository.UpdateAsync(user);
        return NoContent();
    }
}
