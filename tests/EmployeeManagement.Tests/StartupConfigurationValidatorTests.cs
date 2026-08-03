using EmployeeManagement.Api.Configuration;
using Microsoft.Extensions.Configuration;

namespace EmployeeManagement.Tests;

public class StartupConfigurationValidatorTests
{
    [Fact]
    public void Validate_Throws_WhenJwtSecretIsTooShort()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=test;User Id=test;Password=test;",
                ["Jwt:Key"] = "short",
                ["Jwt:Issuer"] = "EmployeeManagement",
                ["Jwt:Audience"] = "EmployeeManagement",
                ["Jwt:ExpiresInMinutes"] = "60",
                ["Jwt:RefreshTokenDays"] = "7"
            })
            .Build();

        var exception = Assert.Throws<InvalidOperationException>(() => StartupConfigurationValidator.Validate(configuration));

        Assert.Contains("Jwt:Key", exception.Message);
    }

    [Fact]
    public void Validate_DoesNotThrow_WhenConfigurationIsValid()
    {
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

        var exception = Record.Exception(() => StartupConfigurationValidator.Validate(configuration));

        Assert.Null(exception);
    }
}
