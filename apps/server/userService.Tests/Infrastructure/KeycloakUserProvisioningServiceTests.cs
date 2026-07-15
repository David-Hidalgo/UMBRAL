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

    private static HttpResponseMessage ActionEmailOk() => new(HttpStatusCode.NoContent);

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WithValidConfig_ReturnsKeycloakUserId()
    {
        // Arrange
        const string expectedId = "created-user-guid-abc";
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserCreated(expectedId), RoleOk(), RoleAssigned(), ActionEmailOk()));

        // Act
        var result = await sut.CreateUserAsync("user@umbral.io", "Test User", "player");

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateUserAsync_WithValidConfig_MakesFiveHttpCalls()
    {
        // Arrange
        var handler = new SequentialHandler(TokenOk(), UserCreated(), RoleOk(), RoleAssigned(), ActionEmailOk());
        var sut = BuildSut(handler);

        // Act
        await sut.CreateUserAsync("user@umbral.io", "Test User", "player");

        // Assert
        handler.CallCount.Should().Be(5);
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
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "player");

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
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "player");

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
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "player");

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
        var act = async () => await sut.CreateUserAsync("existing@umbral.io", "Test", "player");

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
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "player");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*location*");
    }

    // ─── Role assignment ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WithRole_CallsKeycloakRoleAssignmentApi()
    {
        // Arrange — token, create user, get role, assign role, action email
        var handler = new SequentialHandler(TokenOk(), UserCreated(), RoleOk("admin"), RoleAssigned(), ActionEmailOk());
        var sut = BuildSut(handler);

        // Act
        await sut.CreateUserAsync("user@umbral.io", "Test User", "admin");

        // Assert — 5 calls: token + create user + get role + assign role + action email
        handler.CallCount.Should().Be(5);
    }

    [Fact]
    public async Task CreateUserAsync_WhenActionEmailFails_ThrowsInvalidOperationException()
    {
        // Arrange
        var emailFailed = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("SMTP not configured")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserCreated(), RoleOk("operator"), RoleAssigned(), emailFailed));

        // Act
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "operator");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*action email*");
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
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "nonexistent");

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
        var act = async () => await sut.CreateUserAsync("u@u.io", "Test", "admin");

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*assign*role*");
    }

    // ─── GetUsersAsync tests ──────────────────────────────────────────────────

    private static HttpResponseMessage UsersListOk(params (string id, string username, string email, string firstName, long timestamp)[] users)
    {
        var list = new List<object>();
        foreach (var u in users)
        {
            list.Add(new
            {
                id = u.id,
                username = u.username,
                email = u.email,
                firstName = u.firstName,
                lastName = "Test",
                enabled = true,
                createdTimestamp = u.timestamp
            });
        }
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(list)
        };
    }

    [Fact]
    public async Task GetUsersAsync_WithValidConfig_ReturnsSortedUsers()
    {
        // Arrange
        var user1Id = Guid.NewGuid().ToString();
        var user2Id = Guid.NewGuid().ToString();
        var usersResponse = UsersListOk(
            (user1Id, "player1", "player1@umbral.io", "Player One", 1780361721000),
            (user2Id, "admin1", "admin1@umbral.io", "Admin One", 1780361722000)
        );
        var role1Response = RoleOk("player");
        var role2Response = RoleOk("admin");

        var sut = BuildSut(new SequentialHandler(TokenOk(), usersResponse, role1Response, role2Response));

        // Act
        var result = await sut.GetUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        var list = new List<UserService.Application.DTOs.UserDto>(result);
        
        list[0].KeycloakUserId.Should().Be(user2Id);
        list[0].Role.Should().Be("admin");
        list[0].Name.Should().Be("Admin One");

        list[1].KeycloakUserId.Should().Be(user1Id);
        list[1].Role.Should().Be("player");
        list[1].Name.Should().Be("Player One");
    }

    [Fact]
    public async Task GetUsersAsync_WithServiceAccounts_FiltersThemOut()
    {
        // Arrange
        var realUserId = Guid.NewGuid().ToString();
        var serviceAccountUserId = Guid.NewGuid().ToString();
        
        var rawJson = $@"
        [
            {{
                ""id"": ""{realUserId}"",
                ""username"": ""player1"",
                ""email"": ""player1@umbral.io"",
                ""firstName"": ""Player One"",
                ""lastName"": ""Test"",
                ""enabled"": true,
                ""createdTimestamp"": 1780361721000
            }},
            {{
                ""id"": ""{serviceAccountUserId}"",
                ""username"": ""service-account-scoring-service"",
                ""email"": """",
                ""firstName"": """",
                ""lastName"": """",
                ""enabled"": true,
                ""createdTimestamp"": 1780361722000,
                ""serviceAccountClientId"": ""scoring-service""
            }}
        ]";

        var usersResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(rawJson, System.Text.Encoding.UTF8, "application/json")
        };
        var roleResponse = RoleOk("player");

        var sut = BuildSut(new SequentialHandler(TokenOk(), usersResponse, roleResponse));

        // Act
        var result = await sut.GetUsersAsync();

        // Assert
        result.Should().ContainSingle();
        result.Single().KeycloakUserId.Should().Be(realUserId);
    }

    [Fact]
    public async Task GetUsersAsync_WhenUserHasNoValidGuidId_ThrowsInvalidOperationException()
    {
        // Arrange
        var usersResponse = UsersListOk(
            ("non-guid-id", "player1", "player1@umbral.io", "Player One", 1780361721000)
        );
        var roleResponse = RoleOk("player");
        var sut = BuildSut(new SequentialHandler(TokenOk(), usersResponse, roleResponse));

        // Act
        var act = async () => await sut.GetUsersAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*invalid non-GUID*");
    }

    [Fact]
    public async Task GetUsersAsync_WhenKeycloakReturns404_ThrowsInvalidOperationException()
    {
        // Arrange
        var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("Realm not found")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), notFoundResponse));

        // Act
        var act = async () => await sut.GetUsersAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to fetch users*");
    }

    // ─── GetUserByIdAsync ─────────────────────────────────────────────────────

    private static HttpResponseMessage UserByIdOk(string userId, string name = "Test User") =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                id = userId,
                username = "user@umbral.io",
                email = "user@umbral.io",
                firstName = name,
                lastName = "",
                enabled = true,
                emailVerified = false,
                createdTimestamp = 1780361721000L
            })
        };

    [Fact]
    public async Task GetUserByIdAsync_WhenUserExists_ReturnsMappedDto()
    {
        var userId = Guid.NewGuid().ToString();
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserByIdOk(userId, "Test User"), RoleOk("admin")));

        var result = await sut.GetUserByIdAsync(userId);

        result.Should().NotBeNull();
        result!.KeycloakUserId.Should().Be(userId);
        result.Name.Should().Be("Test User");
        result.Role.Should().Be("admin");
        result.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenKeycloakReturns404_ReturnsNull()
    {
        var sut = BuildSut(new SequentialHandler(TokenOk(), new HttpResponseMessage(HttpStatusCode.NotFound)));

        var result = await sut.GetUserByIdAsync("non-existent-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_WhenKeycloakReturnsError_ThrowsInvalidOperationException()
    {
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), errorResponse));

        var act = async () => await sut.GetUserByIdAsync("some-id");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*fetch*");
    }

    // ─── UpdateUserAsync ──────────────────────────────────────────────────────

    private static HttpResponseMessage UserPutOk() => new(HttpStatusCode.NoContent);

    [Fact]
    public async Task UpdateUserAsync_WithValidData_MakesGetThenPutAndSucceeds()
    {
        var userId = Guid.NewGuid().ToString();
        var handler = new SequentialHandler(TokenOk(), UserByIdOk(userId, "Old Name"), UserPutOk());
        var sut = BuildSut(handler);

        await sut.UpdateUserAsync(userId, "New Name");

        handler.CallCount.Should().Be(3); // token + GET + PUT
    }

    [Fact]
    public async Task UpdateUserAsync_WhenGetExistingUserFails_ThrowsInvalidOperationException()
    {
        var getFailed = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("Unavailable")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), getFailed));

        var act = async () => await sut.UpdateUserAsync("some-id", "New Name");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*fetch*");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenPutFails_ThrowsInvalidOperationException()
    {
        var userId = Guid.NewGuid().ToString();
        var putFailed = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad request")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), UserByIdOk(userId), putFailed));

        var act = async () => await sut.UpdateUserAsync(userId, "New Name");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*update failed*");
    }

    // ─── UpdateUserRoleAsync ──────────────────────────────────────────────────

    private static HttpResponseMessage AssignedRolesOk(string roleName = "player") =>
        new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { new { id = "role-guid-old", name = roleName } })
        };

    private static HttpResponseMessage RolesDeleted() => new(HttpStatusCode.NoContent);

    [Fact]
    public async Task UpdateUserRoleAsync_WithValidData_RemovesOldRoleAndAssignsNew()
    {
        // Token → GET assigned roles → DELETE roles → GET available → POST assign
        var handler = new SequentialHandler(
            TokenOk(),
            AssignedRolesOk("player"),
            RolesDeleted(),
            RoleOk("operator"),
            RoleAssigned());
        var sut = BuildSut(handler);

        await sut.UpdateUserRoleAsync(Guid.NewGuid().ToString(), "operator");

        handler.CallCount.Should().Be(5);
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenUserHasNoCurrentRoles_SkipsDeleteAndAssigns()
    {
        // Token → GET assigned roles (empty) → GET available → POST assign
        var emptyRoles = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<object>())
        };
        var handler = new SequentialHandler(TokenOk(), emptyRoles, RoleOk("admin"), RoleAssigned());
        var sut = BuildSut(handler);

        await sut.UpdateUserRoleAsync(Guid.NewGuid().ToString(), "admin");

        handler.CallCount.Should().Be(4); // no DELETE call
    }

    [Fact]
    public async Task UpdateUserRoleAsync_WhenDeleteFails_ThrowsInvalidOperationException()
    {
        var deleteFailed = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("Forbidden")
        };
        var sut = BuildSut(new SequentialHandler(TokenOk(), AssignedRolesOk("player"), deleteFailed));

        var act = async () => await sut.UpdateUserRoleAsync(Guid.NewGuid().ToString(), "admin");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*remove*");
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
