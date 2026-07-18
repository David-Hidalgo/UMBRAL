using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using LiveSessionService.Infrastructure.Messaging;
using Xunit;

namespace LiveSessionService.Tests.Infrastructure
{
    public class RabbitMqEventBusTests
    {
        private const string ExchangeName = "umbral.livesessions";

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
            var @event = new { Test = "Value" };

            channelMock
                .Setup(c => c.BasicPublishAsync(
                    ExchangeName,
                    @event.GetType().Name,
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
                    @event.GetType().Name,
                    false,
                    It.IsAny<BasicProperties>(),
                    It.IsAny<ReadOnlyMemory<byte>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task PublishAsync_AfterDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            var channelMock = new Mock<IChannel>(MockBehavior.Strict);
            var sut = BuildSut(connectionMock, channelMock);
            channelMock.Setup(c => c.Dispose());
            connectionMock.Setup(c => c.Dispose());
            sut.Dispose();

            // Act
            Func<Task> act = async () => await sut.PublishAsync(new { Test = "Value" });

            // Assert
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_IsIdempotent()
        {
            // Arrange
            var connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            var channelMock = new Mock<IChannel>(MockBehavior.Strict);
            var sut = BuildSut(connectionMock, channelMock);
            channelMock.Setup(c => c.Dispose());
            connectionMock.Setup(c => c.Dispose());

            // Act
            sut.Dispose();
            sut.Dispose();

            // Assert
            channelMock.Verify(c => c.Dispose(), Times.Once);
            connectionMock.Verify(c => c.Dispose(), Times.Once);
        }

        [Fact]
        public void Constructor_WithNoBroker_ThrowsException()
        {
            // Arrange
            var options = Options.Create(new RabbitMqOptions
            {
                Host = "invalid-host-name-non-existent",
                Port = 5672,
                Username = "guest",
                Password = "guest",
                ExchangeName = "test-exchange"
            });

            // Act
            Action act = () => new RabbitMqEventBus(options);

            // Assert
            act.Should().Throw<Exception>();
        }
    }
}
