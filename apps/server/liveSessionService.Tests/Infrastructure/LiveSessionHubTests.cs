using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;
using FluentAssertions;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Repositories;
using LiveSessionService.Infrastructure.Notifications.Hubs;

namespace LiveSessionService.Tests.Infrastructure
{
    public class LiveSessionHubTests
    {
        private readonly Mock<ILiveSessionRepository> _repositoryMock = new();
        private readonly Mock<HubCallerContext> _contextMock = new();
        private readonly Mock<IGroupManager> _groupsMock = new();
        private readonly LiveSessionHub _hub;

        public LiveSessionHubTests()
        {
            _hub = new LiveSessionHub(_repositoryMock.Object)
            {
                Context = _contextMock.Object,
                Groups = _groupsMock.Object
            };
            _contextMock.Setup(c => c.ConnectionId).Returns("connection-abc");
        }

        private void SetupUserClaims(string? keycloakUserId)
        {
            if (keycloakUserId == null)
            {
                _contextMock.Setup(c => c.User).Returns((ClaimsPrincipal)null!);
                return;
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, keycloakUserId)
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
            _contextMock.Setup(c => c.User).Returns(principal);
        }

        [Fact]
        public async Task JoinTeamGroup_WithValidUserAndTeam_ShouldAddToGroup()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            var userId = "user-123";
            SetupUserClaims(userId);

            var team = Team.Create(teamId, "Alfa", Guid.NewGuid());
            var player = Player.Create(Guid.NewGuid(), userId, "Ana", teamId);
            team.AddPlayer(player);

            _repositoryMock
                .Setup(r => r.GetTeamByIdAsync(teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(team);

            // Act
            Func<Task> act = () => _hub.JoinTeamGroup(teamId);

            // Assert
            await act.Should().NotThrowAsync();
            _groupsMock.Verify(g => g.AddToGroupAsync("connection-abc", $"team-{teamId}", It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task JoinTeamGroup_WithMissingUserClaim_ShouldThrowHubException()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            SetupUserClaims(null);

            // Act
            Func<Task> act = () => _hub.JoinTeamGroup(teamId);

            // Assert
            await act.Should().ThrowAsync<HubException>()
                .WithMessage("No se pudo identificar al usuario desde el token JWT.");
            _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task JoinTeamGroup_WithNonExistentTeam_ShouldThrowHubException()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            SetupUserClaims("user-123");

            _repositoryMock
                .Setup(r => r.GetTeamByIdAsync(teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Team)null!);

            // Act
            Func<Task> act = () => _hub.JoinTeamGroup(teamId);

            // Assert
            await act.Should().ThrowAsync<HubException>()
                .WithMessage("El equipo especificado no pertenece a ninguna sesión o no existe.");
            _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task JoinTeamGroup_WithUserNotInTeam_ShouldThrowHubException()
        {
            // Arrange
            var teamId = Guid.NewGuid();
            SetupUserClaims("user-not-in-team");

            var team = Team.Create(teamId, "Alfa", Guid.NewGuid());
            var player = Player.Create(Guid.NewGuid(), "user-123", "Ana", teamId);
            team.AddPlayer(player);

            _repositoryMock
                .Setup(r => r.GetTeamByIdAsync(teamId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(team);

            // Act
            Func<Task> act = () => _hub.JoinTeamGroup(teamId);

            // Assert
            await act.Should().ThrowAsync<HubException>()
                .WithMessage("No tienes permiso para unirte a este equipo.");
            _groupsMock.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
