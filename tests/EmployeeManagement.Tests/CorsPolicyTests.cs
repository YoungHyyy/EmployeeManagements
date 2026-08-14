using EmployeeManagement.Api.Extensions;

namespace EmployeeManagement.Tests;

public class CorsPolicyTests
{
    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:5173")]
    [InlineData("http://127.0.0.1:4200")]
    [InlineData("https://localhost:8080")]
    public void Allows_Localhost_Origins(string origin)
    {
        Assert.True(ServiceCollectionExtensions.IsAllowedOrigin(origin, Array.Empty<string>()));
    }

    [Fact]
    public void Allows_Configured_NonLocal_Origin()
    {
        Assert.True(ServiceCollectionExtensions.IsAllowedOrigin(
            "https://app.example.com",
            new[] { "https://app.example.com" }));
    }

    [Fact]
    public void Rejects_Unknown_Remote_Origin()
    {
        Assert.False(ServiceCollectionExtensions.IsAllowedOrigin(
            "https://evil.example",
            Array.Empty<string>()));
    }
}
