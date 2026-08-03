using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Api.Services;
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
    private readonly IAvatarUploadService _avatarUploadService;

    public ProfileController(IUserRepository userRepository, IAvatarUploadService avatarUploadService)
    {
        _userRepository = userRepository;
        _avatarUploadService = avatarUploadService;
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

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var email = User.Identity?.Name ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null) return NotFound();

        try
        {
            var avatarPath = await _avatarUploadService.SaveAsync(file, Environment.CurrentDirectory);
            user.AvatarUrl = avatarPath;
            await _userRepository.UpdateAsync(user);

            return Ok(new { success = true, message = "Tải ảnh đại diện thành công", data = new { avatarUrl = avatarPath } });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message, data = (object?)null });
        }
    }
}
