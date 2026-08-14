using EmployeeManagement.Api.Middleware;
using FluentAssertions;

namespace EmployeeManagement.Tests.Middleware;

public class RequestLoggingMiddlewareTests
{
    [Fact]
    public void MaskSensitive_ShouldHidePasswordAndTokens()
    {
        var raw = """
            {"email":"a@mail.com","password":"Secret123","accessToken":"aaa","data":{"refreshToken":"bbb"}}
            """;

        var masked = RequestLoggingMiddleware.MaskSensitive(raw);

        masked.Should().Contain("a@mail.com");
        masked.Should().Contain("***");
        masked.Should().NotContain("Secret123");
        masked.Should().NotContain("aaa");
        masked.Should().NotContain("bbb");
    }
}
