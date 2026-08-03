using EmployeeManagement.Api.Extensions;
using EmployeeManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Tests;

public class DependencyRegistrationTests
{
    [Fact]
    public void AddApplicationServices_RegistersTokenService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=test;User Id=test;Password=test;",
                ["Jwt:Key"] = "ThisIsAReallyLongAndSecureJwtSigningKey123!",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagement",
                ["Jwt:ExpiresInMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        services.AddSingleton<IConfiguration>(configuration);
        services.AddApplicationServices(configuration);

        using var provider = services.BuildServiceProvider();
        var tokenService = provider.GetService<ITokenService>();

        Assert.NotNull(tokenService);
    }
}
