using System.Linq;
using System.Reflection;
using api.Infrastructure.Controllers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace api.Tests;

public class UsersControllerAuthorizationTests
{
    [Fact]
    public void UsersController_HasAuthorizeAttribute()
    {
        var type = typeof(UsersController);
        var att = type.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(att);
    }

    [Fact]
    public void CreateMethod_HasAuthorizeRolesAdmin()
    {
        var type = typeof(UsersController);
        var method = type.GetMethod("Create");
        Assert.NotNull(method);

        var att = method.GetCustomAttributes<AuthorizeAttribute>(true).FirstOrDefault();
        Assert.NotNull(att);
        Assert.Equal("admin", att.Roles);
    }
}
