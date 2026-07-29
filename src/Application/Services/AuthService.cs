using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _configuration;
        private readonly IRoleRepository _roleRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;

        public AuthService(
            IDbConnectionFactory dbFactory, 
            IConfiguration configuration, 
            IRoleRepository roleRepository,
            IRefreshTokenRepository refreshTokenRepository)
        {
            _dbFactory = dbFactory;
            _configuration = configuration;
            _roleRepository = roleRepository;
            _refreshTokenRepository = refreshTokenRepository;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (!string.Equals(request.Password, request.ConfirmPassword, StringComparison.Ordinal))
                return new AuthResponse { Success = false, Message = "Password and confirm password do not match" };

            using var conn = _dbFactory.CreateConnection();
            var exists = await conn.QueryFirstOrDefaultAsync<int?>("SELECT 1 FROM Users WHERE Email = @Email LIMIT 1", new { request.Email });
            if (exists.HasValue)
                return new AuthResponse { Success = false, Message = "Email already exists" };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var sql = @"INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, CreatedAt)
                        VALUES (@FullName, @Email, @PasswordHash, @PhoneNumber, NOW()); SELECT LAST_INSERT_ID();";
            var userId = await conn.ExecuteScalarAsync<int>(sql, new { request.FullName, request.Email, PasswordHash = passwordHash, request.PhoneNumber });

            var employeeRoleId = await _roleRepository.GetRoleIdByCodeAsync("EMPLOYEE");
            if (employeeRoleId.HasValue)
            {
                await _roleRepository.AssignRoleAsync(userId, employeeRoleId.Value);
            }

            var accessToken = GenerateJwtToken(userId, request.Email, "EMPLOYEE");
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

            return new AuthResponse { Success = true, Message = "Registered", AccessToken = accessToken, RefreshToken = rawRefreshToken, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            using var conn = _dbFactory.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT Id, Email, PasswordHash FROM Users WHERE Email = @Email LIMIT 1", new { request.Email });
            if (user == null)
                return new AuthResponse { Success = false, Message = "Invalid credentials" };

            var passwordHash = (string)user.PasswordHash;
            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
                return new AuthResponse { Success = false, Message = "Invalid credentials" };

            var roleCode = await GetUserRoleCodeAsync((int)user.Id);
            var accessToken = GenerateJwtToken((int)user.Id, (string)user.Email, roleCode);
            var rawRefreshToken = Guid.NewGuid().ToString("N");
            var refreshTokenHash = HashToken(rawRefreshToken);

            await _refreshTokenRepository.AddAsync(new RefreshToken
            {
                UserId = (int)user.Id,
                TokenHash = refreshTokenHash,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                IsUsed = false,
                IsRevoked = false,
                CreatedAt = DateTime.UtcNow
            });

            return new AuthResponse { Success = true, Message = "Logged in", AccessToken = accessToken, RefreshToken = rawRefreshToken, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return new AuthResponse { Success = false, Message = "Invalid refresh token" };

            var tokenHash = HashToken(refreshToken);
            var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

            if (storedToken == null || storedToken.IsRevoked || storedToken.IsUsed || storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                return new AuthResponse { Success = false, Message = "Invalid refresh token" };
            }

            using var conn = _dbFactory.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT Id, Email FROM Users WHERE Id = @Id AND IsDeleted = 0 LIMIT 1", new { Id = storedToken.UserId });
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

            var roleCode = await GetUserRoleCodeAsync((int)user.Id);
            var accessToken = GenerateJwtToken((int)user.Id, (string)user.Email, roleCode);

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

            using var conn = _dbFactory.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT Id, PasswordHash FROM Users WHERE Id = @Id AND IsDeleted = 0 LIMIT 1", new { Id = userId });
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var currentHash = (string)user.PasswordHash;
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, currentHash))
                return new AuthResponse { Success = false, Message = "Current password is incorrect" };

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await conn.ExecuteAsync("UPDATE Users SET PasswordHash = @PasswordHash, UpdatedAt = NOW() WHERE Id = @Id", new { PasswordHash = newHash, Id = userId });

            return new AuthResponse { Success = true, Message = "Password changed successfully" };
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            using var conn = _dbFactory.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT Id, Email FROM Users WHERE Email = @Email AND IsDeleted = 0 LIMIT 1", new { request.Email });
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var otp = new Random().Next(100000, 999999).ToString();
            await conn.ExecuteAsync("UPDATE Users SET OtpCode = @OtpCode, OtpExpiration = DATE_ADD(NOW(), INTERVAL 10 MINUTE) WHERE Id = @Id", new { OtpCode = otp, Id = (int)user.Id });
            return new AuthResponse { Success = true, Message = $"OTP generated: {otp}" };
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            if (request.NewPassword != request.ConfirmPassword)
                return new AuthResponse { Success = false, Message = "New password and confirm password do not match" };

            using var conn = _dbFactory.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync<dynamic>("SELECT Id, OtpCode, OtpExpiration FROM Users WHERE Email = @Email AND IsDeleted = 0 LIMIT 1", new { request.Email });
            if (user == null)
                return new AuthResponse { Success = false, Message = "User not found" };

            var storedOtp = (string?)user.OtpCode;
            var otpExpiration = (DateTime?)user.OtpExpiration;

            if (string.IsNullOrWhiteSpace(storedOtp) || !string.Equals(storedOtp, request.OtpCode, StringComparison.Ordinal))
                return new AuthResponse { Success = false, Message = "Invalid OTP code" };

            if (!otpExpiration.HasValue || otpExpiration.Value < DateTime.Now)
                return new AuthResponse { Success = false, Message = "OTP has expired" };

            var newHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await conn.ExecuteAsync(@"UPDATE Users SET PasswordHash = @PasswordHash, OtpCode = NULL, OtpExpiration = NULL, UpdatedAt = NOW() WHERE Id = @Id", new { PasswordHash = newHash, Id = (int)user.Id });

            return new AuthResponse { Success = true, Message = "Password reset successfully" };
        }

        private async Task<string> GetUserRoleCodeAsync(int userId)
        {
            using var conn = _dbFactory.CreateConnection();
            var roleCode = await conn.ExecuteScalarAsync<string?>(@"SELECT r.Code FROM UserRoles ur
                JOIN Roles r ON ur.RoleId = r.Id
                WHERE ur.UserId = @UserId AND ur.IsDeleted = 0 AND r.IsDeleted = 0 LIMIT 1", new { UserId = userId });
            return roleCode ?? "EMPLOYEE";
        }

        private string GenerateJwtToken(int userId, string email, string roleCode)
        {
            var jwt = _configuration.GetSection("Jwt");
            var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key not configured");
            var issuer = jwt["Issuer"];
            var audience = jwt["Audience"];
            var expiresMinutes = int.TryParse(jwt["ExpiresInMinutes"], out var m) ? m : 60;

            var tokenHandler = new JwtSecurityTokenHandler();
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Role, roleCode)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string HashToken(string rawToken)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawToken));
            return Convert.ToHexString(bytes);
        }
    }
}
