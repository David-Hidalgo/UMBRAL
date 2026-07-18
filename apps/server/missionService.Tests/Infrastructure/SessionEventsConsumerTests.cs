using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MissionService.Domain.Entities;
using MissionService.Infrastructure.Messaging;
using MissionService.Infrastructure.Messaging.Consumers;
using MissionService.Infrastructure.Persistence;
using Moq;
using Xunit;

namespace MissionService.Tests.Infrastructure
{
    public class SessionEventsConsumerTests
    {
        private readonly Mock<ILogger<SessionEventsConsumer>> _loggerMock;
        private readonly IOptions<RabbitMqOptions> _options;

        public SessionEventsConsumerTests()
        {
            _loggerMock = new Mock<ILogger<SessionEventsConsumer>>();
            _options = Options.Create(new RabbitMqOptions
            {
                Host = "localhost",
                Port = 5672,
                Username = "guest",
                Password = "guest",
                ExchangeName = "test-exchange"
            });
        }

        private MissionDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<MissionDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new MissionDbContext(options);
        }

        [Fact]
        public async Task ProcessMessageAsync_SessionActivated_IncrementsSessionCount()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var subStageId = Guid.NewGuid();
            var subStage = TriviaSubStage.Create(subStageId, "Trivia", 1, 10, Question.Create(Guid.NewGuid(), "Q?", new List<Answer> { Answer.Create(Guid.NewGuid(), "A", true) }), null);
            dbContext.SubStages.Add(subStage);
            await dbContext.SaveChangesAsync();

            var serviceProvider = CreateMockServiceProvider(dbContext);

            var consumer = new SessionEventsConsumer(serviceProvider, _loggerMock.Object, _options);

            var @event = new SessionActivatedIntegrationEvent(Guid.NewGuid(), new List<Guid> { subStageId });
            var messageJson = JsonSerializer.Serialize(@event);

            // Act
            await InvokeProcessMessageAsync(consumer, nameof(SessionActivatedIntegrationEvent), messageJson);

            // Assert
            var updatedSubStage = await dbContext.SubStages.FindAsync(subStageId);
            updatedSubStage.Should().NotBeNull();
            updatedSubStage!.Status.ActiveSessionCount.Should().Be(1);
        }

        [Fact]
        public async Task ProcessMessageAsync_SessionFinished_DecrementsSessionCount()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var subStageId = Guid.NewGuid();
            var subStage = TriviaSubStage.Create(subStageId, "Trivia", 1, 10, Question.Create(Guid.NewGuid(), "Q?", new List<Answer> { Answer.Create(Guid.NewGuid(), "A", true) }), null);
            subStage.IncrementSessionCount(); // Start at 1
            dbContext.SubStages.Add(subStage);
            await dbContext.SaveChangesAsync();

            var serviceProvider = CreateMockServiceProvider(dbContext);

            var consumer = new SessionEventsConsumer(serviceProvider, _loggerMock.Object, _options);

            var @event = new SessionFinishedIntegrationEvent(Guid.NewGuid(), new List<Guid> { subStageId });
            var messageJson = JsonSerializer.Serialize(@event);

            // Act
            await InvokeProcessMessageAsync(consumer, nameof(SessionFinishedIntegrationEvent), messageJson);

            // Assert
            var updatedSubStage = await dbContext.SubStages.FindAsync(subStageId);
            updatedSubStage.Should().NotBeNull();
            updatedSubStage!.Status.ActiveSessionCount.Should().Be(0);
        }

        [Fact]
        public async Task ProcessMessageAsync_SessionCancelled_DecrementsSessionCount()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var subStageId = Guid.NewGuid();
            var subStage = TriviaSubStage.Create(subStageId, "Trivia", 1, 10, Question.Create(Guid.NewGuid(), "Q?", new List<Answer> { Answer.Create(Guid.NewGuid(), "A", true) }), null);
            subStage.IncrementSessionCount(); // Start at 1
            dbContext.SubStages.Add(subStage);
            await dbContext.SaveChangesAsync();

            var serviceProvider = CreateMockServiceProvider(dbContext);

            var consumer = new SessionEventsConsumer(serviceProvider, _loggerMock.Object, _options);

            var @event = new SessionCancelledIntegrationEvent(Guid.NewGuid(), new List<Guid> { subStageId });
            var messageJson = JsonSerializer.Serialize(@event);

            // Act
            await InvokeProcessMessageAsync(consumer, nameof(SessionCancelledIntegrationEvent), messageJson);

            // Assert
            var updatedSubStage = await dbContext.SubStages.FindAsync(subStageId);
            updatedSubStage.Should().NotBeNull();
            updatedSubStage!.Status.ActiveSessionCount.Should().Be(0);
        }

        [Fact]
        public async Task ProcessMessageAsync_StageTransitioned_DecrementsPreviousAndIncrementsNew()
        {
            // Arrange
            using var dbContext = CreateInMemoryDbContext();
            var prevSubStageId = Guid.NewGuid();
            var newSubStageId = Guid.NewGuid();

            var prevSubStage = TriviaSubStage.Create(prevSubStageId, "Prev Trivia", 1, 10, Question.Create(Guid.NewGuid(), "Q?", new List<Answer> { Answer.Create(Guid.NewGuid(), "A", true) }), null);
            prevSubStage.IncrementSessionCount();

            var newSubStage = TriviaSubStage.Create(newSubStageId, "New Trivia", 2, 15, Question.Create(Guid.NewGuid(), "Q?", new List<Answer> { Answer.Create(Guid.NewGuid(), "A", true) }), null);

            dbContext.SubStages.AddRange(prevSubStage, newSubStage);
            await dbContext.SaveChangesAsync();

            var serviceProvider = CreateMockServiceProvider(dbContext);

            var consumer = new SessionEventsConsumer(serviceProvider, _loggerMock.Object, _options);

            var @event = new StageTransitionedIntegrationEvent(Guid.NewGuid(), new List<Guid> { prevSubStageId }, new List<Guid> { newSubStageId });
            var messageJson = JsonSerializer.Serialize(@event);

            // Act
            await InvokeProcessMessageAsync(consumer, nameof(StageTransitionedIntegrationEvent), messageJson);

            // Assert
            var updatedPrev = await dbContext.SubStages.FindAsync(prevSubStageId);
            var updatedNew = await dbContext.SubStages.FindAsync(newSubStageId);

            updatedPrev!.Status.ActiveSessionCount.Should().Be(0);
            updatedNew!.Status.ActiveSessionCount.Should().Be(1);
        }

        private IServiceProvider CreateMockServiceProvider(MissionDbContext dbContext)
        {
            var serviceProviderMock = new Mock<IServiceProvider>();
            var serviceScopeMock = new Mock<IServiceScope>();
            var serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

            serviceScopeMock.Setup(s => s.ServiceProvider).Returns(serviceProviderMock.Object);
            serviceProviderMock.Setup(s => s.GetService(typeof(MissionDbContext))).Returns(dbContext);
            serviceProviderMock.Setup(s => s.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactoryMock.Object);
            serviceScopeFactoryMock.Setup(s => s.CreateScope()).Returns(serviceScopeMock.Object);

            return serviceProviderMock.Object;
        }

        private async Task InvokeProcessMessageAsync(SessionEventsConsumer consumer, string routingKey, string message)
        {
            var method = typeof(SessionEventsConsumer).GetMethod("ProcessMessageAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException("ProcessMessageAsync method not found.");
            }

            var task = (Task)method.Invoke(consumer, new object[] { routingKey, message, CancellationToken.None })!;
            await task;
        }
    }
}
