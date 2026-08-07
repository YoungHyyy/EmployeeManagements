using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EmployeeManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            ILogger<AuthService>? logger = null)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _logger = logger ?? NullLogger<AuthService>.Instance;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                return new AuthResponse { Success = false, Message = "Mật khẩu và mật khẩu xác nhận không khớp" };

            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                return new AuthResponse { Success = false, Message = "Email đã tồn tại" };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };
            var userId = await _userRepository.CreateAsync(user);

            var roleCode = string.IsNullOrWhiteSpace(request.RoleCode) ? "EMPLOYEE" : request.RoleCode.ToUpperInvariant();
            var roleId = await _roleRepository.GetRoleIdByCodeAsync(roleCode);
            if (roleId.HasValue)
            {
                await _roleRepository.AssignRoleAsync(userId, roleId.Value);
            }
            else
            {
                var defaultRoleId = await _roleRepository.GetRoleIdByCodeAsync("EMPLOYEE");
                if (defaultRoleId.HasValue)
                    await _roleRepository.AssignRoleAsync(userId, defaultRoleId.Value);
                roleCode = "EMPLOYEE";
            }

            var accessToken = _tokenService.GenerateAccessToken(userId, request.Email, roleCode);
            var rawRefreshToken = Guid.NewGuid().ToString("N");
            var refreshTokenHash = HashToken(rawRefreshToken);

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = userId,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),// Set the expiration to 7 days from now
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            return new AuthResponse { Success = true, Message = "Đăng ký thành công", AccessToken = accessToken, RefreshToken = rawRefreshToken, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed for email {Email}: user not found", request.Email);
                return new AuthResponse { Success = false, Message = "Thông tin đăng nhập không hợp lệ" };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for email {Email}: invalid password", request.Email);
                return new AuthResponse { Success = false, Message = "Thông tin đăng nhập không hợp lệ" };
            }

            var roleCode = await GetUserRoleCodeAsync(user.Id);
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, roleCode);
            var rawRefreshToken = Guid.NewGuid().ToString("N");
            var refreshTokenHash = HashToken(rawRefreshToken);

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("Login succeeded for email {Email} with role {Role}", user.Email, roleCode);
            return new AuthResponse { Success = true, Message = "Đăng nhập thành công", AccessToken = accessToken, RefreshToken = rawRefreshToken, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return new AuthResponse { Success = false, Message = "Refresh token không hợp lệ" };

            var tokenHash = HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null || storedToken.IsRevoked || storedToken.IsUsed || storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new AuthResponse { Success = false, Message = "Refresh token không hợp lệ" };
            }

            var user = await _userRepository.GetByIdAsync(storedToken.UserId);
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var newRawRefreshToken = Guid.NewGuid().ToString("N");
            var newTokenHash = HashToken(newRawRefreshToken);

            await _refreshTokenRepository.MarkAsUsedAsync(storedToken.Id, newTokenHash);
            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = storedToken.UserId,
                TokenHash = newTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            var roleCode = await GetUserRoleCodeAsync(user.Id);
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, roleCode);

            return new AuthResponse { Success = true, Message = "Refreshed", AccessToken = accessToken, RefreshToken = newRawRefreshToken, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        public async Task<AuthResponse> LogoutAsync(string refreshToken)
        {
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var tokenHash = HashToken(refreshToken);
                await _refreshTokenRepository.RevokeAsync(tokenHash);
            }
            return new AuthResponse { Success = true, Message = "Logged out" };
        }

        public async Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return new AuthResponse { Success = false, Message = "New password and confirm password do not match" };

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return new AuthResponse { Success = false, Message = "Current password is incorrect" };

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordHash = newHash;
            await _userRepository.UpdateAsync(user);

            return new AuthResponse { Success = true, Message = "Password changed successfully" };
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.UpdateAsync(user);
            return new AuthResponse { Success = true, Message = $"OTP generated: {otp}" };
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return new AuthResponse { Success = false, Message = "New password and confirm password do not match" };

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var storedOtp = user.OtpCode;
            var otpExpiration = user.OtpExpiration;

            if (string.IsNullOrWhiteSpace(storedOtp) || !string.Equals(storedOtp, request.OtpCode, StringComparison.Ordinal))
                return new AuthResponse { Success = false, Message = "Invalid OTP code" };

            if (!otpExpiration.HasValue || otpExpiration.Value < DateTime.UtcNow)
                return new AuthResponse { Success = false, Message = "OTP has expired" };

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.PasswordHash = newHash;
            user.OtpCode = null;
            user.OtpExpiration = null;
            await _userRepository.UpdateAsync(user);

            return new AuthResponse { Success = true, Message = "Password reset successfully" };
        }

        private async Task<string> GetUserRoleCodeAsync(int userId)
        {
            var roleCode = await _roleRepository.GetRoleCodeByUserIdAsync(userId);
            return roleCode ?? "EMPLOYEE";
        }

        private static string HashToken(string rawToken)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}
