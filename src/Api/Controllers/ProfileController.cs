using System.Security.Claims;
using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers;

/// <summary>
/// Presentation layer: only IProfileService. No repository, no try/catch.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.EmployeeOrAdmin)]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IWebHostEnvironment _env;

    public ProfileController(IProfileService profileService, IWebHostEnvironment env)
    {
        _profileService = profileService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        var email = GetCurrentUserEmail();
        var profile = await _profileService.GetByEmailAsync(email);
        return profile == null
            ? NotFound(ApiResponse.Fail("Không tìm thấy hồ sơ người dùng"))
            : Ok(profile);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request)
    {
        var email = GetCurrentUserEmail();
        await _profileService.UpdateByEmailAsync(email, request);
        return Ok(ApiResponse.Ok(message: "Cập nhật hồ sơ thành công"));
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var email = GetCurrentUserEmail();
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file ảnh hợp lệ.");
        }

        await using var stream = file.OpenReadStream();
        var storageRoot = string.IsNullOrWhiteSpace(_env.WebRootPath)
            ? Path.Combine(_env.ContentRootPath, "wwwroot")
            : _env.WebRootPath;

        var avatarPath = await _profileService.UploadAvatarByEmailAsync(
            email,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            storageRoot);

        return Ok(ApiResponse.Ok(new { avatarUrl = avatarPath }, "Tải ảnh đại diện thành công"));
    }

    private string GetCurrentUserEmail()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value
                    ?? User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new UnauthorizedAccessException("Chưa đăng nhập hoặc token không hợp lệ");
        }

        return email;
    }
}
