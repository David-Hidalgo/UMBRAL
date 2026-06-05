using System;
using System.Threading.Tasks;
using FluentAssertions;
using UserService.Application.IntegrationEvents;
using UserService.Infrastructure.Messaging;
using Xunit;

namespace UserService.Tests.Infrastructure;

public class NullEventBusTests
{
    private readonly NullEventBus _sut = new();

    [Fact]
    public async Task PublishAsync_WithAnyEvent_CompletesWithoutThrowing()
    {
        // Arrange
        var @event = new UserCreated(Guid.NewGuid(), "kc-123", "test@umbral.io", "player");

        // Act
        var act = async () => await _sut.PublishAsync(@event);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_CompletesWithoutThrowing()
    {
        // Act
        var act = async () => await _sut.PublishAsync<object?>(null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishAsync_MultipleTimes_AlwaysCompletesSuccessfully()
    {
        // Arrange
        var event1 = new UserCreated(Guid.NewGuid(), "kc-1", "user1@umbral.io", "player");
        var event2 = new UserCreated(Guid.NewGuid(), "kc-2", "user2@umbral.io", "admin");

        // Act & Assert
        await _sut.Invoking(b => b.PublishAsync(event1)).Should().NotThrowAsync();
        await _sut.Invoking(b => b.PublishAsync(event2)).Should().NotThrowAsync();
    }
}
