using System;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using EmployeeManagement.Application.DTOs;
using EmployeeManagement.Application.Interfaces;
using EmployeeManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EmployeeManagement.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IDbConnectionFactory _dbFactory;
        private readonly IConfiguration _configuration;

        public AuthService(IDbConnectionFactory dbFactory, IConfiguration configuration)
        {
            _dbFactory = dbFactory;
            _configuration = configuration;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            using var conn = _dbFactory.CreateConnection();
            var exists = await conn.QueryFirstOrDefaultAsync<int?>("SELECT 1 FROM Users WHERE Email = @Email LIMIT 1", new { request.Email });
            if (exists.HasValue)
                return new AuthResponse { Success = false, Message = "Email already exists" };

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var sql = @"INSERT INTO Users (FullName, Email, PasswordHash, PhoneNumber, CreatedAt)
                        VALUES (@FullName, @Email, @PasswordHash, @PhoneNumber, NOW()); SELECT LAST_INSERT_ID();";
            var userId = await conn.ExecuteScalarAsync<int>(sql, new { request.FullName, request.Email, PasswordHash = passwordHash, request.PhoneNumber });

            var token = GenerateJwtToken(request.Email);

            return new AuthResponse { Success = true, Message = "Registered", AccessToken = token, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            using var conn = _dbFactory.CreateConnection();
            var user = await conn.QueryFirstOrDefaultAsync("SELECT Id, Email, PasswordHash FROM Users WHERE Email = @Email LIMIT 1", new { request.Email });
            if (user == null)
                return new AuthResponse { Success = false, Message = "Invalid credentials" };

            var passwordHash = (string)user.PasswordHash;
            if (!BCrypt.Net.BCrypt.Verify(request.Password, passwordHash))
                return new AuthResponse { Success = false, Message = "Invalid credentials" };

            var token = GenerateJwtToken(request.Email);

            return new AuthResponse { Success = true, Message = "Logged in", AccessToken = token, ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds() };
        }

        private string GenerateJwtToken(string email)
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
                Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Email, email) }),
                Expires = DateTime.UtcNow.AddMinutes(expiresMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
