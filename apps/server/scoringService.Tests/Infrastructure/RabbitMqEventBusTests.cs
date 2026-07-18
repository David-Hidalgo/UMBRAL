using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ScoringService.Application.IntegrationEvents;
using ScoringService.Infrastructure.Messaging;
using Testcontainers.RabbitMq;
using Moq;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    /// <summary>
    /// Integration tests for RabbitMqEventBus using a real RabbitMQ container via Testcontainers.
    /// Verifies that events published through the bus are correctly routed and received by queues.
    /// </summary>
    [Trait("Category", "Integration")]
    public sealed class RabbitMqEventBusTests : IAsyncLifetime
    {
        private readonly RabbitMqContainer _rabbit = new RabbitMqBuilder()
            .WithImage("rabbitmq:3-management")
            .WithUsername("umbral_user")
            .WithPassword("umbral_pass")
            .Build();

        public async Task InitializeAsync()
        {
            if (Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") != "true" || !DockerHelper.IsDockerAvailable)
                return;

            await _rabbit.StartAsync();
        }

        public async Task DisposeAsync()
        {
            if (Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") == "true" && DockerHelper.IsDockerAvailable)
            {
                await _rabbit.DisposeAsync();
            }
        }

        [IntegrationFact]
        public async Task PublishAsync_Should_PublishEventToRabbitMq()
        {
            // Arrange
            var options = new RabbitMqOptions
            {
                Host = _rabbit.Hostname,
                Port = _rabbit.GetMappedPublicPort(5672),
                Username = "umbral_user",
                Password = "umbral_pass",
                ExchangeName = "umbral.livesessions",
                ScoringExchangeName = "umbral.scoring"
            };
            var optionsWrapper = Options.Create(options);

            // Establish a verification connection to check delivery
            var factory = new ConnectionFactory
            {
                HostName = options.Host,
                Port = options.Port,
                UserName = options.Username,
                Password = options.Password
            };
            using var verificationConnection = await factory.CreateConnectionAsync();
            using var verificationChannel = await verificationConnection.CreateChannelAsync();

            // Initialize our system under test (SUT)
            using var eventBus = new RabbitMqEventBus(optionsWrapper);

            // Declare a test queue and bind it to the exchange
            const string testQueue = "test.scoring.recalculated";
            await verificationChannel.QueueDeclareAsync(
                queue: testQueue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            var routingKey = nameof(ScoreRecalculatedIntegrationEvent);
            await verificationChannel.QueueBindAsync(
                queue: testQueue,
                exchange: options.ScoringExchangeName,
                routingKey: routingKey);

            var @event = new ScoreRecalculatedIntegrationEvent(
                Guid.NewGuid(),
                Guid.NewGuid(),
                15,
                250,
                DateTime.UtcNow);

            // Act
            await eventBus.PublishAsync(@event, CancellationToken.None);

            // Assert
            // Brief wait for message delivery
            await Task.Delay(500);

            var response = await verificationChannel.BasicGetAsync(testQueue, autoAck: true);
            response.Should().NotBeNull();

            var json = Encoding.UTF8.GetString(response!.Body.ToArray());
            var receivedEvent = JsonSerializer.Deserialize<ScoreRecalculatedIntegrationEvent>(json);

            receivedEvent.Should().NotBeNull();
            receivedEvent!.SessionId.Should().Be(@event.SessionId);
            receivedEvent.TeamId.Should().Be(@event.TeamId);
            receivedEvent.Delta.Should().Be(@event.Delta);
            receivedEvent.TotalPoints.Should().Be(@event.TotalPoints);
        }
    }
 
    public class RabbitMqEventBusUnitTests
    {
        private const string ScoringExchangeName = "umbral.scoring";
 
        private static RabbitMqEventBus BuildSut(
            Mock<IConnection> connectionMock,
            Mock<IChannel> channelMock)
        {
            var options = Options.Create(new RabbitMqOptions { ScoringExchangeName = ScoringExchangeName });
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
                    ScoringExchangeName,
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
                    ScoringExchangeName,
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
                ScoringExchangeName = "test-exchange"
            });
 
            // Act
            Action act = () => new RabbitMqEventBus(options);
 
            // Assert
            act.Should().Throw<Exception>();
        }
    }
}
