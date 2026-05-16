using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using shared.Authentication;
using UserService.Infrastructure.Authentication;
using Xunit;

namespace UserService.Tests.Infrastructure;

public class KeycloakUserProvisioningServiceTests
{
    // ─── Builders ─────────────────────────────────────────────────────────────

    private static KeycloakAdminOptions ValidOptions() => new()
    {
        BaseUrl       = "http://keycloak:8080",
        Realm         = "umbral",
        AdminClientId = "user-service-admin",
        AdminClientSecret = "test-secret"
    };

    private static KeycloakUserProvisioningService BuildSut(
        HttpMessageHandler handler,
        KeycloakAdminOptions? options = null)
    {
        var client = new HttpClient(handler);
        var opts   = Options.Create(options ?? ValidOptions());
        return new KeycloakUserProvisioningService(client, opts);
    }

    private static HttpResponseMessage TokenOk() => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new { access_token = "fake-admin-token" })
    };

    private static HttpResponseMessage UserCreated(string userId = "created-user-guid-abc")
    {
        var r = new HttpResponseMessage(HttpStatusCode.Created);
        r.Headers.Location = new Uri(
            $"http://keycloak:8080/admin/realms/umbral/users/{userId}");
        return r;
    }

    private static HttpResponseMessage RoleOk(string roleName = "player") => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(new[] { new { id = "role-guid-123", name = roleName } })
    };

    private static HttpResponseMessage RoleAssigned() => new(HttpStatusCode.NoContent);

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WithValidConfig_ReturnsKeycloakUserId()
    {
        // Arrange
        const string expectedId = "created-user-guid-abc";
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserCreated(expectedId), RoleOk(), RoleAssigned()));

        // Act
        var result = await sut.CreateUserAsync("user@umbral.io", "Test User", "Password1!", "player");

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidConfig_MakesFourHttpCalls()
    {
        // Arrange
        var handler = new SequentialHandler(TokenOk(), UserCreated(), RoleOk(), RoleAssigned());
        var sut = BuildSut(handler);

        // Act
        await sut.CreateUserAsync("user@umbral.io", "Test User", "Password1!", "player");

        // Assert
        handler.CallCount.Should().Be(4);
    }

    // ─── EnsureConfigured ────────────────────────────────────────────────────

    [Theory]
    [InlineData("",                    "umbral", "client-id", "secret")]
    [InlineData("http://keycloak:8080","",        "client-id", "secret")]
    [InlineData("http://keycloak:8080","umbral",  "",          "secret")]
    [InlineData("http://keycloak:8080","umbral",  "client-id", "")]
    public async Task CreateUserAsync_WithIncompleteConfig_ThrowsInvalidOperationException(
        string baseUrl, string realm, string clientId, string secret)
    {
        // Arrange
        var opts = new KeycloakAdminOptions
        {
            BaseUrl = baseUrl, Realm = realm,
            AdminClientId = clientId, AdminClientSecret = secret
        };
        var sut = BuildSut(new SequentialHandler(), opts);

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "Password1!", "player");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*incomplete*");
    }

    // ─── Token failures ───────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WhenTokenEndpointReturns401_ThrowsInvalidOperationException()
    {
        // Arrange
        var tokenFailed = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };
        var sut = BuildSut(new SequentialHandler(tokenFailed));

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "Password1!", "player");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*token*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenTokenBodyIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyToken = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { access_token = "" })
        };
        var sut = BuildSut(new SequentialHandler(emptyToken));

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "Password1!", "player");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*access token*");
    }

    // ─── User creation failures ───────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WhenUserCreationReturns409_ThrowsInvalidOperationException()
    {
        // Arrange
        var conflict = new HttpResponseMessage(HttpStatusCode.Conflict)
        {
            Content = new StringContent("User already exists")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), conflict));

        // Act
        var act = async () => await sut.CreateUserAsync("existing@umbral.io", "Test", "Password1!", "player");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*409*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenLocationHeaderMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var noLocation = new HttpResponseMessage(HttpStatusCode.Created);
        var sut = BuildSut(new SequentialHandler(TokenOk(), noLocation));

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "Password1!", "player");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*location*");
    }

    // ─── Role assignment ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WithRole_CallsKeycloakRoleAssignmentApi()
    {
        // Arrange — token, create user, get role, assign role
        var handler = new SequentialHandler(TokenOk(), UserCreated(), RoleOk("admin"), RoleAssigned());
        var sut = BuildSut(handler);

        // Act
        await sut.CreateUserAsync("user@umbral.io", "Test User", "Password1!", "admin");

        // Assert — 4 calls: token + create user + get role + assign role
        handler.CallCount.Should().Be(4);
    }

    [Fact]
    public async Task CreateUserAsync_WhenRoleRetrievalFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var roleFailed = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Role not found")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserCreated(), roleFailed));

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "Password1!", "nonexistent");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*role*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenRoleAssignmentFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var assignFailed = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("Forbidden")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserCreated(), RoleOk("admin"), assignFailed));

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "Password1!", "admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assign*role*");
    }

    // ─── SequentialHandler test double ────────────────────────────────────────

    private sealed class SequentialHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _queue;
        public int CallCount { get; private set; }

        public SequentialHandler(params HttpResponseMessage[] responses)
            => _queue = new Queue<HttpResponseMessage>(responses);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (_queue.Count == 0)
                throw new InvalidOperationException(
                    $"SequentialHandler: no response configured for call #{CallCount}.");
            return Task.FromResult(_queue.Dequeue());
        }
    }
}
