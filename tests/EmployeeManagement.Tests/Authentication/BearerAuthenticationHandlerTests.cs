using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EmployeeManagement.Api.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace EmployeeManagement.Tests.Authentication;

public class BearerAuthenticationHandlerTests
{
    [Fact]
    public async Task HandleAuthenticateAsync_WithValidJwt_ReturnsSuccess()
    {
        var handler = CreateHandler();
        var token = CreateToken("admin@example.com", "ADMIN");
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {token}";
        handler.InitializeAsync(new AuthenticationScheme("Test", null, typeof(BearerAuthenticationHandler)), context).GetAwaiter().GetResult();

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.FindFirst(ClaimTypes.Email)!.Value.Should().Be("admin@example.com");
        result.Principal.FindFirst(ClaimTypes.Role)!.Value.Should().Be("ADMIN");
    }

    [Fact]
    public async Task HandleAuthenticateAsync_WithInvalidJwt_ReturnsFailure()
    {
        var handler = CreateHandler();
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer invalid-token";
        handler.InitializeAsync(new AuthenticationScheme("Test", null, typeof(BearerAuthenticationHandler)), context).GetAwaiter().GetResult();

        var result = await handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    private static BearerAuthenticationHandler CreateHandler()
    {
        var options = Options.Create(new AuthenticationSchemeOptions());
        var optionsMonitor = new TestOptionsMonitor<AuthenticationSchemeOptions>(options);
        return new BearerAuthenticationHandler(optionsMonitor, NullLoggerFactory.Instance, new TestUrlEncoder());
    }

    private static string CreateToken(string email, string role)
    {
        var key = Encoding.UTF8.GetBytes("12345678901234567890123456789012");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "EmployeeManagement",
            audience: "EmployeeManagement",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
    {
        private readonly T _value;

        public TestOptionsMonitor(T value) => _value = value;

        public T CurrentValue => _value;
        public T Get(string? name) => _value;
        public IDisposable OnChange(Action<T, string?> listener) => NullDisposable.Instance;
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }

    private sealed class TestUrlEncoder : UrlEncoder
    {
        public TestUrlEncoder() : base() { }
    }
}
