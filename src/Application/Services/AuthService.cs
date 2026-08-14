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
        private readonly IEmployeeRepository? _employeeRepository;
        private readonly IDepartmentRepository? _departmentRepository;
        private readonly IPositionRepository? _positionRepository;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenService tokenService,
            IAuditLogService? auditLogService = null,
            ILogger<AuthService>? logger = null,
            IEmployeeRepository? employeeRepository = null,
            IDepartmentRepository? departmentRepository = null,
            IPositionRepository? positionRepository = null)
        {
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenService = tokenService;
            _auditLogService = auditLogService ?? new NoOpAuditLogService();
            _logger = logger ?? NullLogger<AuthService>.Instance;
            _employeeRepository = employeeRepository;
            _departmentRepository = departmentRepository;
            _positionRepository = positionRepository;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                return new AuthResponse { Success = false, Message = "Mật khẩu và mật khẩu xác nhận không khớp" };

            if (await _userRepository.ExistsByEmailAsync(request.Email))
                return new AuthResponse { Success = false, Message = "Email đã tồn tại" };

            const string roleCode = "EMPLOYEE";
            var roleId = await _roleRepository.GetRoleIdByCodeAsync(roleCode);
            if (!roleId.HasValue)
            {
                return new AuthResponse { Success = false, Message = ApiMessages.RoleNotFound(roleCode) };
            }

            var defaults = await ResolveDefaultCatalogAsync();
            if (defaults.Error is { } catalogError)
            {
                return new AuthResponse { Success = false, Message = catalogError };
            }

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                PasswordHash = passwordHash,
                PhoneNumber = request.PhoneNumber,
                IsActive = true
            };
            var userId = await _userRepository.CreateWithRoleAsync(user, roleId.Value);
            await EnsureEmployeeProfileAsync(userId, request, defaults.Department, defaults.Position);

            var tokens = await IssueTokensAsync(userId, request.Email, roleCode);

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

            return OkAuth("Đăng ký thành công", tokens);
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

            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed for email {Email}: account disabled", request.Email);
                await _auditLogService.WriteAsync(new AuditLogWriteRequest
                {
                    UserId = user.Id,
                    Action = AuditActions.LoginFailed,
                    Module = AuditModules.Auth,
                    EntityName = "User",
                    EntityId = user.Id.ToString(),
                    RequestPath = "/api/Auth/login",
                    Details = $"Email={request.Email}; Reason=AccountDisabled"
                });
                return new AuthResponse { Success = false, Message = ApiMessages.AccountDisabled };
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);

            var roleCode = await GetUserRoleCodeAsync(user.Id);
            var tokens = await IssueTokensAsync(user.Id, user.Email, roleCode);

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

            return OkAuth("Đăng nhập thành công", tokens);
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

            if (!user.IsActive)
            {
                await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);
                return new AuthResponse { Success = false, Message = ApiMessages.AccountDisabled };
            }

            var roleCode = await GetUserRoleCodeAsync(user.Id);
            var tokens = await IssueTokensAsync(user.Id, user.Email, roleCode);
            await _refreshTokenRepository.MarkAsUsedAsync(storedToken.Id, HashToken(tokens.RefreshToken));

            return OkAuth("Làm mới token thành công", tokens);
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
            await _refreshTokenRepository.RevokeAllUserTokensAsync(userId);

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
            {
                return new AuthResponse
                {
                    Success = true,
                    Message = "Nếu email tồn tại, mã OTP đã được gửi (giả lập)"
                };
            }

            var otp = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
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
            await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id);

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

        private async Task<AuthTokenDto> IssueTokensAsync(int userId, string email, string roleCode)
        {
            var accessToken = _tokenService.GenerateAccessToken(userId, email, roleCode);
            var rawRefreshToken = Guid.NewGuid().ToString("N");
            var refreshDays = Math.Max(1, _tokenService.GetRefreshTokenDays());
            var expiresMinutes = Math.Max(1, _tokenService.GetAccessTokenExpiresMinutes());

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = userId,
                TokenHash = HashToken(rawRefreshToken),
                ExpiresAt = DateTime.UtcNow.AddDays(refreshDays),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            return new AuthTokenDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken,
                ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiresMinutes).ToUnixTimeSeconds()
            };
        }

        private async Task<(Department? Department, Position? Position, string? Error)> ResolveDefaultCatalogAsync()
        {
            if (_employeeRepository == null)
            {
                return (null, null, null);
            }

            if (_departmentRepository == null || _positionRepository == null)
            {
                return (null, null, ApiMessages.DefaultCatalogMissing);
            }

            var department = await _departmentRepository.GetByCodeAsync(DefaultCatalog.DepartmentCode);
            var position = await _positionRepository.GetByCodeAsync(DefaultCatalog.PositionCode);
            if (department == null || position == null)
            {
                return (null, null, ApiMessages.DefaultCatalogMissing);
            }

            return (department, position, null);
        }

        private async Task EnsureEmployeeProfileAsync(
            int userId,
            RegisterRequest request,
            Department? defaultDepartment,
            Position? defaultPosition)
        {
            if (_employeeRepository == null)
            {
                return;
            }

            var existing = await _employeeRepository.GetByUserIdAsync(userId)
                           ?? await _employeeRepository.GetByEmailAsync(request.Email);

            if (existing != null)
            {
                if (!existing.UserId.HasValue)
                {
                    await _employeeRepository.LinkUserAsync(existing.Id, userId);
                }

                return;
            }

            if (defaultDepartment == null || defaultPosition == null)
            {
                throw new InvalidOperationException(ApiMessages.DefaultCatalogMissing);
            }

            var employee = new Employee
            {
                UserId = userId,
                EmployeeCode = await GenerateUniqueEmployeeCodeAsync(),
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                DepartmentId = defaultDepartment.Id,
                PositionId = defaultPosition.Id,
                HireDate = DateTime.Today,
                Status = "Working",
                Gender = "Other",
                IsActive = true
            };

            await _employeeRepository.CreateAsync(employee);
        }

        private async Task<string> GenerateUniqueEmployeeCodeAsync()
        {
            for (var attempt = 0; attempt < 8; attempt++)
            {
                var date = DateTime.UtcNow.ToString("yyMMdd");
                var code = $"EMP{date}{RandomNumberGenerator.GetInt32(1000, 10000)}";
                if (_employeeRepository == null || !await _employeeRepository.ExistsByEmployeeCodeAsync(code))
                {
                    return code;
                }
            }

            throw new InvalidOperationException("Không thể sinh mã nhân viên, vui lòng thử lại");
        }

        private static AuthResponse OkAuth(string message, AuthTokenDto tokens) => new()
        {
            Success = true,
            Message = message,
            Data = tokens
        };

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
