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
    public void DepartmentsGetAllAction_RequiresEmployeeOrAdminPolicy()
    {
        var method = typeof(DepartmentsController).GetMethod(nameof(DepartmentsController.GetAll));
        Assert.NotNull(method);

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(AuthorizationPolicies.EmployeeOrAdmin, authorizeAttribute!.Policy);
    }

    [Fact]
    public void DepartmentsCreateAction_RequiresAdminOnlyPolicy()
    {
        var method = typeof(DepartmentsController).GetMethod(nameof(DepartmentsController.Create));
        Assert.NotNull(method);

        var authorizeAttribute = method!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .OfType<AuthorizeAttribute>()
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
        Assert.Equal(AuthorizationPolicies.AdminOnly, authorizeAttribute!.Policy);
    }
}
