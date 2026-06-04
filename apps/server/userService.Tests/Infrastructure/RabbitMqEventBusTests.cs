using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using UserService.Application.IntegrationEvents;
using UserService.Infrastructure.Messaging;
using Xunit;

namespace UserService.Tests.Infrastructure;

public class RabbitMqEventBusTests
{
    private const string ExchangeName = "umbral.users";

    private static RabbitMqEventBus BuildSut(
        Mock<IConnection> connectionMock,
        Mock<IChannel> channelMock)
    {
        var options = Options.Create(new RabbitMqOptions { ExchangeName = ExchangeName });
        return new RabbitMqEventBus(connectionMock.Object, channelMock.Object, options);
    }

    [Fact]
    public async Task PublishAsync_WithAnyEvent_CallsBasicPublishOnChannel()
    {
        // Arrange
        var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
        var channelMock = new Mock<IChannel>(MockBehavior.Strict);
        var sut = BuildSut(connectionMock, channelMock);
        var @event = new UserCreated(Guid.NewGuid(), "kc-123", "user@umbral.io", "player");

        channelMock
            .Setup(c => c.BasicPublishAsync(
                ExchangeName,
                nameof(UserCreated),
                false,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        await sut.PublishAsync(@event, CancellationToken.None);

        // Assert
        channelMock.Verify(
            c => c.BasicPublishAsync(
                ExchangeName,
                nameof(UserCreated),
                false,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithEvent_UsesEventTypeNameAsRoutingKey()
    {
        // Arrange
        var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
        var channelMock = new Mock<IChannel>(MockBehavior.Strict);
        var sut = BuildSut(connectionMock, channelMock);
        var @event = new UserDeactivated("kc-456");
        string? capturedRoutingKey = null;

        channelMock
            .Setup(c => c.BasicPublishAsync(
                ExchangeName,
                It.IsAny<string>(),
                false,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, routingKey, _, _, _, _) => capturedRoutingKey = routingKey)
            .Returns(ValueTask.CompletedTask);

        // Act
        await sut.PublishAsync(@event, CancellationToken.None);

        // Assert
        capturedRoutingKey.Should().Be(nameof(UserDeactivated));
    }

    [Fact]
    public async Task PublishAsync_WithEvent_SerializesPayloadAsJson()
    {
        // Arrange
        var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
        var channelMock = new Mock<IChannel>(MockBehavior.Strict);
        var sut = BuildSut(connectionMock, channelMock);
        var userId = Guid.NewGuid();
        var @event = new UserCreated(userId, "kc-789", "test@umbral.io", "admin");
        ReadOnlyMemory<byte> capturedBody = default;

        channelMock
            .Setup(c => c.BasicPublishAsync(
                ExchangeName,
                It.IsAny<string>(),
                false,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, _, _, _, body, _) => capturedBody = body)
            .Returns(ValueTask.CompletedTask);

        // Act
        await sut.PublishAsync(@event, CancellationToken.None);

        // Assert
        var json = Encoding.UTF8.GetString(capturedBody.Span);
        var deserialized = JsonSerializer.Deserialize<UserCreated>(json);
        deserialized.Should().BeEquivalentTo(@event);
    }

    [Fact]
    public async Task PublishAsync_WithEvent_UsesPersistentDeliveryMode()
    {
        // Arrange
        var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
        var channelMock = new Mock<IChannel>(MockBehavior.Strict);
        var sut = BuildSut(connectionMock, channelMock);
        BasicProperties? capturedProperties = null;

        channelMock
            .Setup(c => c.BasicPublishAsync(
                ExchangeName,
                It.IsAny<string>(),
                false,
                It.IsAny<BasicProperties>(),
                It.IsAny<ReadOnlyMemory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, bool, BasicProperties, ReadOnlyMemory<byte>, CancellationToken>(
                (_, _, _, props, _, _) => capturedProperties = props)
            .Returns(ValueTask.CompletedTask);

        // Act
        await sut.PublishAsync(new UserUpdated(Guid.NewGuid(), "kc-1", "New Name", "operator"), CancellationToken.None);

        // Assert
        capturedProperties.Should().NotBeNull();
        capturedProperties!.ContentType.Should().Be("application/json");
        capturedProperties.DeliveryMode.Should().Be(DeliveryModes.Persistent);
    }

    [Fact]
    public void Dispose_ClosesChannelAndConnection()
    {
        // Arrange
        var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
        var channelMock = new Mock<IChannel>(MockBehavior.Strict);
        var sut = BuildSut(connectionMock, channelMock);

        channelMock.Setup(c => c.Dispose());
        connectionMock.Setup(c => c.Dispose());

        // Act
        sut.Dispose();

        // Assert
        channelMock.Verify(c => c.Dispose(), Times.Once);
        connectionMock.Verify(c => c.Dispose(), Times.Once);
    }
}
