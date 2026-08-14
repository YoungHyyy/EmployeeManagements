using EmployeeManagement.Application.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace EmployeeManagement.Tests.Services;

public class TokenServiceTests
{
    [Fact]
    public void GenerateAccessToken_ShouldCreateJwtToken()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super-secret-key-123456-super-secret-key-123456",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagementClient",
                ["Jwt:ExpiresInMinutes"] = "60"
            })
            .Build();

        var service = new TokenService(configuration);

        var token = service.GenerateAccessToken(7, "user@example.com", "ADMIN");

        token.Should().NotBeNullOrWhiteSpace();
        token.Split('.').Should().HaveCount(3);
        service.GetAccessTokenExpiresMinutes().Should().Be(60);
        service.GetRefreshTokenDays().Should().Be(7);
    }

    [Fact]
    public void TokenLifetime_ShouldReadJwtSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:ExpiresInMinutes"] = "15",
                ["Jwt:RefreshTokenDays"] = "3"
            })
            .Build();

        var service = new TokenService(configuration);

        service.GetAccessTokenExpiresMinutes().Should().Be(15);
        service.GetRefreshTokenDays().Should().Be(3);
    }
}
