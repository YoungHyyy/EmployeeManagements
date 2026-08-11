using System.Security.Cryptography;
using System.Text;
using EmployeeManagement.Application.Common;
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
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IAuditLogService? auditLogService = null,
            ILogger<AuthService>? logger = null)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _auditLogService = auditLogService ?? new NoOpAuditLogService();
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

            const string roleCode = "EMPLOYEE";
            var roleId = await _roleRepository.GetRoleIdByCodeAsync(roleCode);
            if (roleId.HasValue)
            {
                await _roleRepository.AssignRoleAsync(userId, roleId.Value);
            }

            var accessToken = _tokenService.GenerateAccessToken(userId, request.Email, roleCode);
            var rawRefreshToken = Guid.NewGuid().ToString("N");
            var refreshTokenHash = HashToken(rawRefreshToken);

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = userId,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = userId,
                Action = AuditActions.Register,
                Module = AuditModules.Auth,
                EntityName = "User",
                EntityId = userId.ToString(),
                RequestPath = "/api/Auth/register",
                Details = $"Email={request.Email}"
            });

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng ký thành công",
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds()
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed for email {Email}: user not found", request.Email);
                await _auditLogService.WriteAsync(new AuditLogWriteRequest
                {
                    UserId = null,
                    Action = AuditActions.LoginFailed,
                    Module = AuditModules.Auth,
                    EntityName = "User",
                    RequestPath = "/api/Auth/login",
                    Details = $"Email={request.Email}; Reason=UserNotFound"
                });
                return new AuthResponse { Success = false, Message = "Thông tin đăng nhập không hợp lệ" };
            }

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for email {Email}: invalid password", request.Email);
                await _auditLogService.WriteAsync(new AuditLogWriteRequest
                {
                    UserId = user.Id,
                    Action = AuditActions.LoginFailed,
                    Module = AuditModules.Auth,
                    EntityName = "User",
                    EntityId = user.Id.ToString(),
                    RequestPath = "/api/Auth/login",
                    Details = $"Email={request.Email}; Reason=InvalidPassword"
                });
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

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = user.Id,
                Action = AuditActions.LoginSuccess,
                Module = AuditModules.Auth,
                EntityName = "User",
                EntityId = user.Id.ToString(),
                RequestPath = "/api/Auth/login",
                Details = $"Email={user.Email}; Role={roleCode}"
            });

            return new AuthResponse
            {
                Success = true,
                Message = "Đăng nhập thành công",
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds()
            };
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
                return new AuthResponse { Success = false, Message = "Không tìm thấy người dùng" };

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

            return new AuthResponse
            {
                Success = true,
                Message = "Làm mới token thành công",
                AccessToken = accessToken,
                RefreshToken = newRawRefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds()
            };
        }

        public async Task<AuthResponse> LogoutAsync(string refreshToken)
        {
            int? userId = null;
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                var tokenHash = HashToken(refreshToken);
                var stored = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);
                userId = stored?.UserId;
                await _refreshTokenRepository.RevokeAsync(tokenHash);
            }

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = userId,
                Action = AuditActions.Logout,
                Module = AuditModules.Auth,
                EntityName = "User",
                EntityId = userId?.ToString(),
                RequestPath = "/api/Auth/logout"
            });

            return new AuthResponse { Success = true, Message = "Đăng xuất thành công" };
        }

        public async Task<AuthResponse> ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return new AuthResponse { Success = false, Message = "Mật khẩu mới và xác nhận mật khẩu không khớp" };

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Không tìm thấy người dùng" };

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
                return new AuthResponse { Success = false, Message = "Mật khẩu hiện tại không đúng" };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = userId,
                Action = AuditActions.ChangePassword,
                Module = AuditModules.Auth,
                EntityName = "User",
                EntityId = userId.ToString(),
                RequestPath = "/api/Auth/change-password"
            });

            return new AuthResponse { Success = true, Message = "Đổi mật khẩu thành công" };
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Không tìm thấy người dùng với email này" };

            var otp = new Random().Next(100000, 999999).ToString();
            user.OtpCode = otp;
            user.OtpExpiration = DateTime.UtcNow.AddMinutes(10);
            await _userRepository.UpdateAsync(user);

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = user.Id,
                Action = AuditActions.ForgotPassword,
                Module = AuditModules.Auth,
                EntityName = "User",
                EntityId = user.Id.ToString(),
                RequestPath = "/api/Auth/forgot-password",
                Details = $"Email={request.Email}"
            });

            return new AuthResponse { Success = true, Message = $"Mã OTP đã được tạo (giả lập): {otp}" };
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return new AuthResponse { Success = false, Message = "Mật khẩu mới và xác nhận mật khẩu không khớp" };

            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                return new AuthResponse { Success = false, Message = "Không tìm thấy người dùng với email này" };

            var storedOtp = user.OtpCode;
            var otpExpiration = user.OtpExpiration;

            if (string.IsNullOrWhiteSpace(storedOtp) || !string.Equals(storedOtp, request.OtpCode, StringComparison.Ordinal))
                return new AuthResponse { Success = false, Message = "Mã OTP không hợp lệ" };

            if (!otpExpiration.HasValue || otpExpiration.Value < DateTime.UtcNow)
                return new AuthResponse { Success = false, Message = "Mã OTP đã hết hạn" };

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.OtpCode = null;
            user.OtpExpiration = null;
            await _userRepository.UpdateAsync(user);

            await _auditLogService.WriteAsync(new AuditLogWriteRequest
            {
                UserId = user.Id,
                Action = AuditActions.ResetPassword,
                Module = AuditModules.Auth,
                EntityName = "User",
                EntityId = user.Id.ToString(),
                RequestPath = "/api/Auth/reset-password",
                Details = $"Email={request.Email}"
            });

            return new AuthResponse { Success = true, Message = "Đặt lại mật khẩu thành công" };
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

        /// <summary>Used when tests construct AuthService without audit DI.</summary>
        private sealed class NoOpAuditLogService : IAuditLogService
        {
            public Task WriteAsync(AuditLogWriteRequest request) => Task.CompletedTask;
            public Task<AuditLogListResult> GetListAsync(AuditLogQuery query)
                => Task.FromResult(new AuditLogListResult());
        }
    }
}
