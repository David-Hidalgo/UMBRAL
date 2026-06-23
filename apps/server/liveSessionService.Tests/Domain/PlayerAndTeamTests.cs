using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Domain.Entities;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Tests.Domain
{
    public class PlayerTests
    {
        [Fact]
        public void Create_WithValidData_ShouldCreatePlayer()
        {
            // Act
            var player = Player.Create(Guid.NewGuid(), "user-1", "Ana", Guid.NewGuid());

            // Assert
            player.KeycloakUserId.Should().Be("user-1");
            player.DisplayName.Should().Be("Ana");
        }

        [Fact]
        public void Create_WithEmptyKeycloakUserId_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => Player.Create(Guid.NewGuid(), "", "Ana", Guid.NewGuid());

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("keycloakUserId");
        }

        [Fact]
        public void Create_WithEmptyDisplayName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => Player.Create(Guid.NewGuid(), "user-1", "  ", Guid.NewGuid());

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("displayName");
        }
    }

    public class TeamTests
    {
        [Fact]
        public void Create_WithEmptyName_ShouldThrowArgumentException()
        {
            // Act
            Action act = () => Team.Create(Guid.NewGuid(), "", Guid.NewGuid());

            // Assert
            act.Should().Throw<ArgumentException>().WithParameterName("name");
        }

        [Fact]
        public void AddDuplicatePlayer_ShouldThrowBusinessException()
        {
            // Arrange
            var team = Team.Create(Guid.NewGuid(), "Alfa", Guid.NewGuid());
            team.AddPlayer(Player.Create(Guid.NewGuid(), "user-1", "Ana", team.Id));

            // Act
            Action act = () => team.AddPlayer(Player.Create(Guid.NewGuid(), "user-1", "Ana 2", team.Id));

            // Assert
            act.Should().Throw<BusinessException>()
                .WithMessage("El jugador ya está registrado en el equipo 'Alfa'.");
        }
    }

    public class BusinessExceptionTests
    {
        [Fact]
        public void Constructor_WithInnerException_ShouldPreserveMessageAndInner()
        {
            // Arrange
            var inner = new InvalidOperationException("inner");

            // Act
            var ex = new BusinessException("outer", inner);

            // Assert
            ex.Message.Should().Be("outer");
            ex.InnerException.Should().Be(inner);
        }
    }
}
