using System.Collections.Generic;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using shared.Authentication;
using Xunit;

namespace shared.Tests.Authentication;

public class CurrentUserServiceTests
{
    private const string AuthenticationType = "Bearer";

    private static CurrentUserService CreateService(HttpContext? httpContext)
    {
        var accessor = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        accessor.SetupGet(a => a.HttpContext).Returns(httpContext);
        return new CurrentUserService(accessor.Object);
    }

    /// <summary>Contexto con identidad autenticada. Sin authenticationType, IsAuthenticated seria false.</summary>
    private static HttpContext AuthenticatedContext(params Claim[] claims)
        => new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, AuthenticationType))
        };

    private static HttpContext AuthenticatedContext(string roleClaimType, params Claim[] claims)
        => new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(claims, AuthenticationType, ClaimTypes.Name, roleClaimType))
        };

    private static HttpContext AnonymousContext()
        => new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) };

    [Fact]
    public void UserId_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var userId = service.UserId;

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenIdentityIsNotAuthenticated_ReturnsNull()
    {
        // Arrange
        var service = CreateService(AnonymousContext());

        // Act
        var userId = service.UserId;

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void UserId_WhenNameIdentifierClaimExists_ReturnsNameIdentifierValue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(
            new Claim(ClaimTypes.NameIdentifier, "id-desde-nameidentifier")));

        // Act
        var userId = service.UserId;

        // Assert
        userId.Should().Be("id-desde-nameidentifier");
    }

    [Fact]
    public void UserId_WhenOnlySubClaimExists_ReturnsSubValue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("sub", "id-desde-sub")));

        // Act
        var userId = service.UserId;

        // Assert
        userId.Should().Be("id-desde-sub");
    }

    [Fact]
    public void UserId_WhenBothNameIdentifierAndSubExist_PrefersNameIdentifier()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(
            new Claim("sub", "id-desde-sub"),
            new Claim(ClaimTypes.NameIdentifier, "id-desde-nameidentifier")));

        // Act
        var userId = service.UserId;

        // Assert
        userId.Should().Be("id-desde-nameidentifier");
    }

    [Fact]
    public void UserId_WhenNoIdentityClaimsExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("irrelevante", "x")));

        // Act
        var userId = service.UserId;

        // Assert
        userId.Should().BeNull();
    }

    [Fact]
    public void UserName_WhenNameClaimTypeExists_ReturnsNameClaimValue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim(ClaimTypes.Name, "operador1")));

        // Act
        var userName = service.UserName;

        // Assert
        userName.Should().Be("operador1");
    }

    [Fact]
    public void UserName_WhenOnlyPreferredUsernameExists_ReturnsPreferredUsername()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("preferred_username", "operador1")));

        // Act
        var userName = service.UserName;

        // Assert
        userName.Should().Be("operador1");
    }

    [Fact]
    public void UserName_WhenServiceAccountHasNoNameClaim_ReturnsPreferredUsername()
    {
        // Arrange: un token de client credentials no trae nombre humano.
        var service = CreateService(AuthenticatedContext(
            new Claim("sub", "service-account-id"),
            new Claim("preferred_username", "service-account-scoring-service")));

        // Act
        var userName = service.UserName;

        // Assert
        userName.Should().Be("service-account-scoring-service");
    }

    [Fact]
    public void UserName_WhenOnlyRawNameClaimExists_ReturnsRawNameValue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("name", "Juan Perez")));

        // Act
        var userName = service.UserName;

        // Assert
        userName.Should().Be("Juan Perez");
    }

    [Fact]
    public void UserName_WhenNoNameClaimsExist_ReturnsNull()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("sub", "solo-id")));

        // Act
        var userName = service.UserName;

        // Assert
        userName.Should().BeNull();
    }

    [Fact]
    public void UserName_WhenHttpContextIsNull_ReturnsNull()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var userName = service.UserName;

        // Assert
        userName.Should().BeNull();
    }

    [Fact]
    public void Roles_WhenHttpContextIsNull_ReturnsEmptyCollection()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var roles = service.Roles;

        // Assert
        roles.Should().BeEmpty();
    }

    [Fact]
    public void Roles_WhenRoleClaimsExist_ReturnsAllRoles()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(
            new Claim(ClaimTypes.Role, "operator"),
            new Claim(ClaimTypes.Role, "admin")));

        // Act
        var roles = service.Roles;

        // Assert
        roles.Should().BeEquivalentTo(new[] { "operator", "admin" });
    }

    [Fact]
    public void Roles_WhenDuplicateRoleClaimsExist_ReturnsDistinctRoles()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(
            new Claim(ClaimTypes.Role, "operator"),
            new Claim(ClaimTypes.Role, "OPERATOR")));

        // Act
        var roles = service.Roles;

        // Assert
        roles.Should().ContainSingle().Which.Should().Be("operator");
    }

    [Fact]
    public void Roles_WhenNoRoleClaimsExist_ReturnsEmptyCollection()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("sub", "solo-id")));

        // Act
        var roles = service.Roles;

        // Assert
        roles.Should().BeEmpty();
    }

    [Fact]
    public void Roles_WhenIdentityUsesCustomRoleClaimType_ReturnsRolesFromThatClaimType()
    {
        // Arrange: fija que se respeta identity.RoleClaimType y no ClaimTypes.Role hardcodeado.
        var service = CreateService(AuthenticatedContext(
            roleClaimType: "custom_roles",
            new Claim("custom_roles", "operator")));

        // Act
        var roles = service.Roles;

        // Assert
        roles.Should().ContainSingle().Which.Should().Be("operator");
    }

    [Fact]
    public void IsAuthenticated_WhenHttpContextIsNull_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(null);

        // Act
        var isAuthenticated = service.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsNotAuthenticated_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(AnonymousContext());

        // Act
        var isAuthenticated = service.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void IsAuthenticated_WhenIdentityIsAuthenticated_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(AuthenticatedContext(new Claim("sub", "id")));

        // Act
        var isAuthenticated = service.IsAuthenticated;

        // Assert
        isAuthenticated.Should().BeTrue();
    }
}
