using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MissionService.Application.DTOs;
using MissionService.Application.Handlers;
using MissionService.Application.Queries;
using MissionService.Domain.Entities;
using MissionService.Domain.RepositoryInterfaces;
using MissionService.Domain.ValueObjects;
using Xunit;

namespace MissionService.Tests.Application
{
    public class GetMissionsHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly GetMissionsHandler _handler;

        public GetMissionsHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new GetMissionsHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Return_Empty_List_When_No_Missions_Exist()
        {
            // Arrange
            var query = new GetMissionsQuery(null, null);

            _repositoryMock
                .Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Mission>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Should_Return_All_Missions_Mapped_To_DTOs()
        {
            // Arrange
            var query = new GetMissionsQuery(null, null);
            var missions = new List<Mission>
            {
                CreateMission("Alpha Mission", "Description A"),
                CreateMission("Beta Mission", "Description B")
            };

            _repositoryMock
                .Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(missions);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dtos = result.ToList();
            dtos.Should().HaveCount(2);
            dtos.Should().AllSatisfy(dto =>
            {
                dto.Id.Should().NotBeEmpty();
                dto.Name.Should().NotBeNullOrEmpty();
                dto.Description.Should().NotBeNullOrEmpty();
                dto.Status.Should().BeOneOf("Active", "Inactive");
                dto.StageCount.Should().BeGreaterThanOrEqualTo(0);
            });
        }

        [Fact]
        public async Task Handle_Should_Map_Mission_Fields_Correctly_To_Summary_DTO()
        {
            // Arrange
            var query = new GetMissionsQuery(null, null);
            var mission = CreateMission("Gamma Mission", "Gamma Description");
            var missions = new List<Mission> { mission };

            _repositoryMock
                .Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(missions);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dto = result.Single();
            dto.Id.Should().Be(mission.Id);
            dto.Name.Should().Be("Gamma Mission");
            dto.Description.Should().Be("Gamma Description");
            dto.Status.Should().Be("Inactive");
            dto.StageCount.Should().Be(0);
        }

        [Fact]
        public async Task Handle_Should_Pass_SearchTerm_Filter_To_Repository()
        {
            // Arrange
            var query = new GetMissionsQuery("Alpha", null);

            _repositoryMock
                .Setup(r => r.GetAllAsync("Alpha", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Mission>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                r => r.GetAllAsync("Alpha", null, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Pass_Status_Filter_To_Repository()
        {
            // Arrange
            var query = new GetMissionsQuery(null, "Active");

            _repositoryMock
                .Setup(r => r.GetAllAsync(null, "Active", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Mission>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                r => r.GetAllAsync(null, "Active", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Pass_Both_Filters_To_Repository()
        {
            // Arrange
            var query = new GetMissionsQuery("test", "Inactive");

            _repositoryMock
                .Setup(r => r.GetAllAsync("test", "Inactive", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Mission>());

            // Act
            await _handler.Handle(query, CancellationToken.None);

            // Assert
            _repositoryMock.Verify(
                r => r.GetAllAsync("test", "Inactive", It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Return_Inactive_Status_String_For_New_Mission()
        {
            // Arrange
            var query = new GetMissionsQuery(null, null);
            var mission = CreateMission("New Mission", "Fresh mission");

            _repositoryMock
                .Setup(r => r.GetAllAsync(null, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Mission> { mission });

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Single().Status.Should().Be("Inactive");
        }

        /// <summary>
        /// Creates a Mission with no stages using its factory method.
        /// All new missions start with Inactive status.
        /// </summary>
        private static Mission CreateMission(string name, string description)
        {
            return Mission.Create(Guid.NewGuid(), name, description, new List<Stage>());
        }
    }
}
