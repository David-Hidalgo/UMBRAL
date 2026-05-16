using System;
using System.Threading.Tasks;
using FluentAssertions;
using UserService.Domain.Entities;
using UserService.Infrastructure.Persistence;
using Xunit;

namespace UserService.Tests.Infrastructure;

public class InMemoryUserRepositoryTests
{
    private readonly InMemoryUserRepository _sut = new();

    private static User MakeUser(string email = "test@umbral.io", string name = "Test User")
        => User.Create(Guid.NewGuid(), "kc-test-123", email, name);

    // ─── AddAsync + GetByIdAsync ──────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ThenGetById_ReturnsSameUser()
    {
        // Arrange
        var user = MakeUser();

        // Act
        await _sut.AddAsync(user);
        var found = await _sut.GetByIdAsync(user.Id);

        // Assert
        found.Should().NotBeNull();
        found!.Id.Should().Be(user.Id);
        found.Email.Value.Should().Be(user.Email.Value);
        found.Name.Value.Should().Be(user.Name.Value);
        found.KeycloakUserId.Should().Be(user.KeycloakUserId);
    }

    [Fact]
    public async Task AddAsync_CompletesSuccessfully()
    {
        // Arrange
        var user = MakeUser();

        // Act
        var act = async () => await _sut.AddAsync(user);

        // Assert
        await act.Should().NotThrowAsync();
    }

    // ─── GetByIdAsync — not found ─────────────────────────────────────────────

    [Fact]
    public async Task GetById_WithUnknownId_ReturnsNull()
    {
        // Act
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    // ─── Multiple users ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_MultipleUsers_EachRetrievableById()
    {
        // Arrange
        var user1 = MakeUser("user1@umbral.io", "User One");
        var user2 = MakeUser("user2@umbral.io", "User Two");
        var user3 = MakeUser("user3@umbral.io", "User Three");

        // Act
        await _sut.AddAsync(user1);
        await _sut.AddAsync(user2);
        await _sut.AddAsync(user3);

        var found1 = await _sut.GetByIdAsync(user1.Id);
        var found2 = await _sut.GetByIdAsync(user2.Id);
        var found3 = await _sut.GetByIdAsync(user3.Id);

        // Assert
        found1!.Email.Value.Should().Be("user1@umbral.io");
        found2!.Email.Value.Should().Be("user2@umbral.io");
        found3!.Email.Value.Should().Be("user3@umbral.io");
    }

    // ─── Overwrite same ID ────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_WithSameIdTwice_OverwritesPreviousEntry()
    {
        // Arrange
        var sharedId = Guid.NewGuid();
        var original    = User.Create(sharedId, "kc-original", "original@umbral.io", "Original");
        var replacement = User.Create(sharedId, "kc-updated",  "updated@umbral.io",  "Updated");

        // Act
        await _sut.AddAsync(original);
        await _sut.AddAsync(replacement);
        var found = await _sut.GetByIdAsync(sharedId);

        // Assert
        found.Should().NotBeNull();
        found!.KeycloakUserId.Should().Be("kc-updated");
        found.Email.Value.Should().Be("updated@umbral.io");
    }
}
