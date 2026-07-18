using System;
using FluentAssertions;
using ScoringService.Infrastructure.Persistence.Inbox;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    public class ProcessedEventTests
    {
        [Fact]
        public void Create_WithValidArguments_SetsAllProperties()
        {
            // Arrange
            var id = Guid.NewGuid();
            const string routingKey = "SessionCreatedIntegrationEvent";
            var processedAt = DateTime.UtcNow;

            // Act
            var processedEvent = ProcessedEvent.Create(id, routingKey, processedAt);

            // Assert
            processedEvent.Id.Should().Be(id);
            processedEvent.RoutingKey.Should().Be(routingKey);
            processedEvent.ProcessedAt.Should().Be(processedAt);
        }

        [Fact]
        public void Create_TwiceWithSameId_ProducesTwoIndependentInstances()
        {
            // Arrange
            var id = Guid.NewGuid();
            var now = DateTime.UtcNow;

            // Act
            var e1 = ProcessedEvent.Create(id, "EventA", now);
            var e2 = ProcessedEvent.Create(id, "EventB", now.AddSeconds(1));

            // Assert
            e1.RoutingKey.Should().Be("EventA");
            e2.RoutingKey.Should().Be("EventB");
            e1.Should().NotBeSameAs(e2);
        }

        [Fact]
        public void Create_ReturnsNonNullInstance()
        {
            var result = ProcessedEvent.Create(Guid.NewGuid(), "SomeEvent", DateTime.UtcNow);
            result.Should().NotBeNull();
        }
    }
}
