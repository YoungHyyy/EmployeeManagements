using EmployeeManagement.Api.Authentication;
using EmployeeManagement.Api.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeManagement.Tests;

public class ControllerAuthorizationTests
{
    [Fact]
    public void RegisterAction_AllowsAnonymous()
    {
        var method = typeof(AuthController).GetMethod(nameof(AuthController.Register));
        Assert.NotNull(method);

        var allowAnonymous = method!
            .GetCustomAttributes(typeof(AllowAnonymousAttribute), true)
            .Any();

        Assert.True(allowAnonymous);
    }

    [Fact]
    public void DepartmentsController_RequiresAdminOnlyPolicy()
    {
        var authorizeAttribute = typeof(DepartmentsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorizeAttribute!.Policy);
    }

    [Fact]
    public void PositionsController_RequiresAdminOnlyPolicy()
    {
        var authorizeAttribute = typeof(PositionsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorizeAttribute!.Policy);
    }

    [Fact]
    public void UsersController_RequiresAdminOnlyPolicy()
    {
        var authorizeAttribute = typeof(UsersController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorizeAttribute!.Policy);
    }

}
