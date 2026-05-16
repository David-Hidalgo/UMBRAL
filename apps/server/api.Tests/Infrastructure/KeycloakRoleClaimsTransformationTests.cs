using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using shared.Authentication;
using api.Infrastructure.Authentication;
using Xunit;

namespace api.Tests.Infrastructure;

public class KeycloakRoleClaimsTransformationTests
{
    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static KeycloakRoleClaimsTransformation Build(string audience = "")
    {
        var options = Options.Create(new KeycloakOptions
        {
            BaseUrl  = "http://keycloak:8080",
            Realm    = "umbral",
            Audience = audience
        });
        return new KeycloakRoleClaimsTransformation(options);
    }

    private static ClaimsPrincipal AuthenticatedPrincipal(params Claim[] extra)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,           "testuser"),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };
        claims.AddRange(extra);
        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    private static ClaimsPrincipal UnauthenticatedPrincipal()
        => new(new ClaimsIdentity());

    private static Claim RealmAccess(params string[] roles)
        => new("realm_access", JsonSerializer.Serialize(new { roles }));

    private static Claim ResourceAccess(string clientId, params string[] roles)
    {
        var data = new Dictionary<string, object> { [clientId] = new { roles } };
        return new("resource_access", JsonSerializer.Serialize(data));
    }

    private static IEnumerable<string> GetRoles(ClaimsPrincipal principal)
        => principal.FindAll(ClaimTypes.Role).Select(c => c.Value);

    // ─── Unauthenticated ──────────────────────────────────────────────────────

    [Fact]
    public async Task TransformAsync_WithUnauthenticatedPrincipal_ReturnsPrincipalUnchanged()
    {
        // Arrange
        var sut       = Build();
        var principal = UnauthenticatedPrincipal();

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        result.Should().BeSameAs(principal);
        GetRoles(result).Should().BeEmpty();
    }

    // ─── realm_access ─────────────────────────────────────────────────────────

    [Fact]
    public async Task TransformAsync_WithRealmAccessRoles_AddsRolesToIdentity()
    {
        // Arrange
        var sut = Build();
        var principal = AuthenticatedPrincipal(RealmAccess("admin", "operator"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().Contain("admin").And.Contain("operator");
    }

    [Fact]
    public async Task TransformAsync_WithNoRealmAccessClaim_AddsNoRoles()
    {
        // Arrange
        var sut       = Build();
        var principal = AuthenticatedPrincipal();

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithRealmAccessMissingRolesKey_AddsNoRoles()
    {
        // Arrange
        var sut = Build();
        var claim = new Claim("realm_access", "{\"permissions\":[\"read\"]}");
        var principal = AuthenticatedPrincipal(claim);

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().BeEmpty();
    }

    [Fact]
    public async Task TransformAsync_WithRealmAccessRolesAsNonArray_AddsNoRoles()
    {
        // Arrange
        var sut = Build();
        var claim = new Claim("realm_access", "{\"roles\":\"admin\"}");
        var principal = AuthenticatedPrincipal(claim);

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().BeEmpty();
    }

    // ─── resource_access ──────────────────────────────────────────────────────

    [Fact]
    public async Task TransformAsync_WithResourceAccessAndMatchingAudience_AddsAudienceRoles()
    {
        // Arrange
        var sut = Build(audience: "my-client");
        var principal = AuthenticatedPrincipal(ResourceAccess("my-client", "player"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().Contain("player");
    }

    [Fact]
    public async Task TransformAsync_WithResourceAccessAndNoAudience_AddsRolesFromAllClients()
    {
        // Arrange — audience vacía → itera todos los clientes
        var sut = Build(audience: "");
        var principal = AuthenticatedPrincipal(ResourceAccess("client-a", "admin"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().Contain("admin");
    }

    [Fact]
    public async Task TransformAsync_WithAudienceNotFoundInResourceAccess_FallsBackToAllClients()
    {
        // Arrange — audience configurada pero ausente en resource_access → fallback a todos
        var sut = Build(audience: "account");
        var principal = AuthenticatedPrincipal(ResourceAccess("other-client", "operator"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().Contain("operator");
    }

    [Fact]
    public async Task TransformAsync_WithEmptyResourceAccessClaim_AddsNoRoles()
    {
        // Arrange
        var sut = Build();
        var claim = new Claim("resource_access", "   ");
        var principal = AuthenticatedPrincipal(claim);

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().BeEmpty();
    }

    // ─── Deduplication ────────────────────────────────────────────────────────

    [Fact]
    public async Task TransformAsync_WithDuplicateRoleInRealm_AddsRoleOnlyOnce()
    {
        // Arrange — el mismo rol en realm_access dos veces (si el JSON lo permite)
        var sut = Build();
        var principal = AuthenticatedPrincipal(RealmAccess("admin", "admin"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Count(r => r == "admin").Should().Be(1);
    }

    [Fact]
    public async Task TransformAsync_WithRoleAlreadyInIdentity_DoesNotDuplicate()
    {
        // Arrange — rol ya presente como claim directo antes de la transformación
        var sut = Build();
        var existing = new Claim(ClaimTypes.Role, "admin");
        var principal = AuthenticatedPrincipal(existing, RealmAccess("admin"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Count(r => r == "admin").Should().Be(1);
    }

    // ─── Combined sources ─────────────────────────────────────────────────────

    [Fact]
    public async Task TransformAsync_WithBothRealmAndResourceAccess_AddsBothSets()
    {
        // Arrange
        var sut = Build(audience: "");
        var principal = AuthenticatedPrincipal(
            RealmAccess("admin"),
            ResourceAccess("my-client", "player"));

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().Contain("admin").And.Contain("player");
    }

    [Fact]
    public async Task TransformAsync_WithMultipleClientsInResourceAccess_AddsRolesFromAll()
    {
        // Arrange
        var data = new Dictionary<string, object>
        {
            ["client-a"] = new { roles = new[] { "admin" } },
            ["client-b"] = new { roles = new[] { "player" } }
        };
        var claim = new Claim("resource_access", JsonSerializer.Serialize(data));
        var sut   = Build(audience: "");
        var principal = AuthenticatedPrincipal(claim);

        // Act
        var result = await sut.TransformAsync(principal);

        // Assert
        GetRoles(result).Should().Contain("admin").And.Contain("player");
    }
}
