using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MissionService.Application.Commands;
using MissionService.Application.Handlers;
using MissionService.Domain.Entities;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;
using Xunit;

namespace MissionService.Tests.Application
{
    public class CreateMissionHandlerTests
    {
        private readonly Mock<IMissionRepository> _repositoryMock;
        private readonly CreateMissionHandler _handler;

        public CreateMissionHandlerTests()
        {
            _repositoryMock = new Mock<IMissionRepository>(MockBehavior.Strict);
            _handler = new CreateMissionHandler(_repositoryMock.Object);
        }

        [Fact]
        public async Task Handle_Should_Create_Mission_Successfully_On_Happy_Path()
        {
            // Arrange
            var command = ValidCommand();
            
            _repositoryMock.Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Mission>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().NotBeEmpty();
            _repositoryMock.Verify(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Mission>(m => m.Name == command.Name && m.Description == command.Description), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_Should_Throw_DuplicateMissionNameException_When_Name_Exists()
        {
            // Arrange
            var command = ValidCommand();

            _repositoryMock.Setup(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<DuplicateMissionNameException>();
            _repositoryMock.Verify(r => r.ExistsByNameAsync(command.Name, It.IsAny<CancellationToken>()), Times.Once);
        }

        private CreateMissionCommand ValidCommand()
        {
            return new CreateMissionCommand(
                "Super Mission",
                "Description of the mission",
                new List<CreateStageCommand>
                {
                    new CreateStageCommand(
                        "Stage 1",
                        "Description of stage 1",
                        0,
                        new List<CreateSubStageCommand>
                        {
                            new CreateSubStageCommand(
                                "Trivia SubStage",
                                0,
                                "Trivia",
                                new List<CreateQuestionCommand>
                                {
                                    new CreateQuestionCommand(
                                        "Which team has won most World Cups?",
                                        15,
                                        new List<CreateAnswerCommand>
                                        {
                                            new CreateAnswerCommand("Brazil", true),
                                            new CreateAnswerCommand("Germany", false)
                                        }
                                    )
                                },
                                null,
                                null
                            ),
                            new CreateSubStageCommand(
                                "Treasure SubStage",
                                1,
                                "TreasureHunt",
                                null,
                                "Find the hidden keys in the room",
                                new List<CreateClueCommand>
                                {
                                    new CreateClueCommand("Look under the desk", 0, 5)
                                }
                            )
                        }
                    )
                }
            );
        }
    }
}
