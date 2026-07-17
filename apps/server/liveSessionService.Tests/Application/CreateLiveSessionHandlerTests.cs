using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Handlers;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.IntegrationEvents;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Tests.Application
{
    public class CreateLiveSessionHandlerTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<IMissionServiceClient> _missionClientMock = new();
        private readonly Mock<IEventBus> _eventBusMock = new();
        private readonly CreateLiveSessionHandler _handler;

        public CreateLiveSessionHandlerTests()
        {
            _handler = new CreateLiveSessionHandler(
                _repositoryMock.Object,
                _missionClientMock.Object,
                _eventBusMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidActiveMission_ShouldCreateSessionAndPublishEvent()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var jwtToken = "valid-token";
            var command = new CreateLiveSessionCommand(missionId, "Partida 1", null, jwtToken);

            var missionDto = new MissionClientDto(
                missionId,
                "Misión Mágica",
                "Active",
                60,
                new List<StageClientDto>()
            );

            _missionClientMock.Setup(x => x.GetMissionByIdAsync(missionId, jwtToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(missionDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _eventBusMock.Verify(x => x.PublishAsync(
                It.Is<SessionCreatedIntegrationEvent>(e => e.MissionId == missionId && e.Name == "Partida 1"),
                It.IsAny<CancellationToken>()
            ), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInactiveMission_ShouldThrowBusinessException()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var jwtToken = "valid-token";
            var command = new CreateLiveSessionCommand(missionId, "Partida 1", null, jwtToken);

            var missionDto = new MissionClientDto(
                missionId,
                "Misión Mágica",
                "Inactive", // Inactive
                60,
                new List<StageClientDto>()
            );

            _missionClientMock.Setup(x => x.GetMissionByIdAsync(missionId, jwtToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(missionDto);

            // Act & Assert
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La misión debe estar activa para poder crear una sesión de juego.");

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenMissionNotFound_ShouldThrowBusinessException()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var command = new CreateLiveSessionCommand(missionId, "Partida 1", null, "jwt");

            _missionClientMock
                .Setup(x => x.GetMissionByIdAsync(missionId, "jwt", It.IsAny<CancellationToken>()))
                .ReturnsAsync((MissionClientDto?)null);

            // Act
            Func<Task> act = () => _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("La misión especificada no existe.");

            _repositoryMock.Verify(x => x.AddAsync(It.IsAny<LiveSession>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
