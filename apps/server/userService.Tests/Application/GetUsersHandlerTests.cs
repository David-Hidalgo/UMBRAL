using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using UserService.Application.DTOs;
using UserService.Application.Handlers;
using UserService.Application.Interfaces;
using UserService.Application.Queries;
using Xunit;

namespace UserService.Tests.Application;

public class GetUsersHandlerTests
{
    private readonly Mock<IIdentityProviderService> _identityMock;
    private readonly GetUsersHandler _sut;

    public GetUsersHandlerTests()
    {
        _identityMock = new Mock<IIdentityProviderService>(MockBehavior.Strict);
        _sut = new GetUsersHandler(_identityMock.Object);
    }

    // ── Happy-path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidQuery_ReturnsUsersFromIdentityProvider()
    {
        // Arrange
        var query = new GetUsersQuery("search", "player");
        var expectedUsers = new List<UserDto>
        {
            new(Guid.NewGuid(), "kc-1", "user@umbral.io", "Test User", "player", DateTime.UtcNow, IsEnabled: true)
        };

        _identityMock
            .Setup(s => s.GetUsersAsync(query.SearchTerm, query.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedUsers);
        _identityMock.Verify(s => s.GetUsersAsync(query.SearchTerm, query.Role, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullSearchTermAndNullRole_DelegatesNullsToIdentityProvider()
    {
        // Arrange
        var query = new GetUsersQuery(null, null);
        var expectedUsers = new List<UserDto>
        {
            new(Guid.NewGuid(), "kc-2", "admin@umbral.io", "Admin User", "admin", DateTime.UtcNow, IsEnabled: true),
            new(Guid.NewGuid(), "kc-3", "op@umbral.io", "Op User", "operator", DateTime.UtcNow, IsEnabled: true)
        };

        _identityMock
            .Setup(s => s.GetUsersAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        _identityMock.Verify(s => s.GetUsersAsync(null, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenIdentityProviderReturnsEmptyList_ReturnsEmptyEnumerable()
    {
        // Arrange
        var query = new GetUsersQuery("nonexistent", "admin");

        _identityMock
            .Setup(s => s.GetUsersAsync(query.SearchTerm, query.Role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserDto>());

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenIdentityProviderThrows_PropagatesException()
    {
        // Arrange
        var query = new GetUsersQuery(null, null);

        _identityMock
            .Setup(s => s.GetUsersAsync(null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak unavailable"));

        // Act
        var act = async () => await _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Keycloak unavailable");
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenToIdentityProvider()
    {
        // Arrange
        var query = new GetUsersQuery(null, null);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _identityMock
            .Setup(s => s.GetUsersAsync(null, null, token))
            .ReturnsAsync(new List<UserDto>());

        // Act
        await _sut.Handle(query, token);

        // Assert — token must be forwarded, not swallowed
        _identityMock.Verify(s => s.GetUsersAsync(null, null, token), Times.Once);
    }
}
