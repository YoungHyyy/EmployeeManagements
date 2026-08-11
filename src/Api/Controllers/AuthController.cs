using System.Security.Claims;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmployeeManagement.Api.Controllers
{
    /// <summary>
    /// Auth API — mã HTTP:
    /// 200 = thành công | 400 = sai dữ liệu/MK | 401 = thiếu/sai JWT (API có [Authorize])
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Consumes("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [AllowAnonymous]
        [HttpPost("register")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return result.Success
                ? StatusCode(StatusCodes.Status200OK, result)
                : StatusCode(StatusCodes.Status400BadRequest, new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
        }

        /// <summary>
        /// Đăng nhập. OK = 200 + token. Sai email/MK = 400 (không phải 401).
        /// </summary>
        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);

            if (result.Success)
            {
                // Ép rõ 200 — tránh Swagger/filter lệch mã khi test lại cùng API
                return StatusCode(StatusCodes.Status200OK, result);
            }

            // Cùng schema AuthResponse (success=false) + 400
            return StatusCode(StatusCodes.Status400BadRequest, new AuthResponse
            {
                Success = false,
                Message = result.Message
            });
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RefreshToken([FromBody] string refreshToken)
        {
            var result = await _authService.RefreshTokenAsync(refreshToken);
            return result.Success
                ? StatusCode(StatusCodes.Status200OK, result)
                : StatusCode(StatusCodes.Status400BadRequest, new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] string refreshToken)
        {
            var result = await _authService.LogoutAsync(refreshToken);
            return Ok(result);
        }

        [Authorize]
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object?>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            var result = await _authService.ChangePasswordAsync(userId, request);
            return result.Success
                ? StatusCode(StatusCodes.Status200OK, result)
                : StatusCode(StatusCodes.Status400BadRequest, new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            return result.Success
                ? StatusCode(StatusCodes.Status200OK, result)
                : StatusCode(StatusCodes.Status400BadRequest, new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            return result.Success
                ? StatusCode(StatusCodes.Status200OK, result)
                : StatusCode(StatusCodes.Status400BadRequest, new AuthResponse
                {
                    Success = false,
                    Message = result.Message
                });
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Chưa đăng nhập hoặc token không hợp lệ");
            }

            return userId;
        }
    }
}
