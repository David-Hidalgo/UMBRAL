using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests;

/// <summary>
/// Pruebas de integración que verifican que las políticas de autorización configuradas
/// en YARP (appsettings.json → ReverseProxy.Routes[*].AuthorizationPolicy) bloquean
/// o permiten solicitudes según el rol del usuario ANTES de reenviar al downstream.
///
/// El gateway rechaza con 401/403 por sí mismo. Cuando autoriza, la petición llega al
/// backend stub de <see cref="StubBackendFixture"/>, que siempre responde 200.
/// </summary>
public class YarpAuthorizationTests : IClassFixture<StubBackendFixture>
{
    private readonly WebApplicationFactory<Program> _factory;

    public YarpAuthorizationTests(StubBackendFixture fixture)
    {
        _factory = fixture.Factory;
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>Crea un cliente HTTP con un token de prueba que incluye los roles indicados.</summary>
    private HttpClient CreateClientWithRoles(params string[] roles)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        var token = string.Join(",", roles); // FakeAuthHandler interpreta "admin,operator" → dos roles
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>Crea un cliente HTTP sin ningún token de autenticación.</summary>
    private HttpClient CreateAnonymousClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    // ─── users-admin (GET/POST/DELETE → AdminOnly) ────────────────────────────

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task UsersAdminRoute_WithAdminRole_IsNotRejectedByGateway(string method)
    {
        // Arrange
        using var client = CreateClientWithRoles("admin");
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/users/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert — un 200 sólo puede venir del stub, así que la autorización pasó en el borde.
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task UsersAdminRoute_WithOperatorRole_ReturnsForbidden(string method)
    {
        // Arrange
        using var client = CreateClientWithRoles("operator");
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/users/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task UsersAdminRoute_WithNoToken_ReturnsUnauthorized(string method)
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/users/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── users-update (PUT → Authenticated) ───────────────────────────────────

    [Fact]
    public async Task UsersUpdateRoute_WithAnyAuthenticatedUser_IsNotRejectedByGateway()
    {
        // Arrange — un usuario sin roles específicos aún está autenticado
        using var client = CreateClientWithRoles();
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Fact]
    public async Task UsersUpdateRoute_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── missions (AdminOrOperator) ───────────────────────────────────────────

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    public async Task MissionsRoute_WithAdminOrOperatorRole_IsNotRejectedByGateway(string role)
    {
        // Arrange
        using var client = CreateClientWithRoles(role);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/missions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Fact]
    public async Task MissionsRoute_WithOnlyAuthenticatedRole_ReturnsForbidden()
    {
        // Arrange — usuario autenticado sin admin ni operator
        using var client = CreateClientWithRoles();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/missions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MissionsRoute_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/missions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── sessions-join (POST → Authenticated) ─────────────────────────────────

    [Fact]
    public async Task SessionsJoinRoute_WithAnyAuthenticatedUser_IsNotRejectedByGateway()
    {
        // Arrange
        using var client = CreateClientWithRoles();
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{sessionId}/teams/{teamId}/join");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Fact]
    public async Task SessionsJoinRoute_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var sessionId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/sessions/{sessionId}/teams/{teamId}/join");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── sessions-admin-operator (POST/PATCH/DELETE → AdminOrOperator) ────────

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    public async Task SessionsAdminOperatorRoute_WithCorrectRole_IsNotRejectedByGateway(string role)
    {
        // Arrange
        using var client = CreateClientWithRoles(role);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sessions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Fact]
    public async Task SessionsAdminOperatorRoute_WithOnlyAuthenticatedRole_ReturnsForbidden()
    {
        // Arrange
        using var client = CreateClientWithRoles();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/sessions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ─── sessions-authenticated (GET → Authenticated) ─────────────────────────

    [Fact]
    public async Task SessionsGetRoute_WithAnyAuthenticatedUser_IsNotRejectedByGateway()
    {
        // Arrange
        using var client = CreateClientWithRoles();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sessions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Fact]
    public async Task SessionsGetRoute_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/sessions/some-path");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ─── sessionhub (WebSocket endpoint → Authenticated) ─────────────────────

    [Fact]
    public async Task SessionHub_WithAuthenticatedUser_IsNotRejectedByGateway()
    {
        // Arrange
        using var client = CreateClientWithRoles();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hubs/sessions/negotiate");

        // Act
        var response = await client.SendAsync(request);

        // Assert — el gateway no rechaza y la petición llega al stub
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "el gateway debe autorizar la petición y reenviarla al backend stub");
    }

    [Fact]
    public async Task SessionHub_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        using var client = CreateAnonymousClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/hubs/sessions/negotiate");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
