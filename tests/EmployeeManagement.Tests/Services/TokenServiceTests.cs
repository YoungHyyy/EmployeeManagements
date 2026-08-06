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
    }
}
