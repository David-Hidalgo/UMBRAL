using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Domain.Entities;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class TreasureHuntSubStageTests
    {
        [Fact]
        public void Clue_Should_Be_Created_Successfully_Without_PenaltyPoints()
        {
            // Arrange & Act
            var clue = Clue.Create(Guid.NewGuid(), "Look under the red chair", 1);

            // Assert
            clue.Should().NotBeNull();
            clue.Text.Should().Be("Look under the red chair");
            clue.Order.Should().Be(1);
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Be_Created_Successfully_With_SecretCode()
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Under the table", 0)
            };

            // Act
            var substage = TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Find the Secret Key",
                0,
                100,
                "Find the key to escape",
                "secret-key-123",
                clues
            );

            // Assert
            substage.Should().NotBeNull();
            substage.Title.Should().Be("Find the Secret Key");
            substage.Points.Should().Be(100);
            substage.Objective.Should().Be("Find the key to escape");
            substage.SecretCode.Should().Be("SECRET-KEY-123"); // Upper-case normalization
            substage.Clues.Should().HaveCount(1);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_SecretCode_Is_Empty(string? invalidCode)
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Under the table", 0)
            };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Find the Secret Key",
                0,
                100,
                "Find the key to escape",
                invalidCode!,
                clues
            );

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Secret code cannot be empty*");
        }

        [Theory]
        [InlineData("secret key")] // has space
        [InlineData("secret@123")] // special character
        [InlineData("secret_key")] // underscore is not allowed in alphanumeric/hyphen match
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_SecretCode_Is_Invalid_Format(string invalidCode)
        {
            // Arrange
            var clues = new List<Clue>
            {
                Clue.Create(Guid.NewGuid(), "Under the table", 0)
            };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(),
                "Find the Secret Key",
                0,
                100,
                "Find the key to escape",
                invalidCode,
                clues
            );

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*must be alphanumeric and can contain hyphens*");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_Title_Is_Invalid(string? invalidTitle)
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), invalidTitle!, 0, 100, "Find the key to escape", "SECRET-123", clues);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_Points_Are_Negative()
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find the Secret Key", 0, -1, "Find the key to escape", "SECRET-123", clues);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_Objective_Is_Invalid(string? invalidObjective)
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find the Secret Key", 0, 100, invalidObjective!, "SECRET-123", clues);

            // Assert
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Throw_ArgumentException_When_SecretCode_Exceeds_100_Characters()
        {
            // Arrange
            var longCode = new string('A', 101);
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };

            // Act
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find the Secret Key", 0, 100, "Find the key", longCode, clues);

            // Assert
            act.Should().Throw<ArgumentException>().WithMessage("*Secret code cannot exceed 100 characters*");
        }

        [Fact]
        public void SessionCount_Operations_Should_Modify_Status_Correctly()
        {
            // Arrange
            var clues = new List<Clue> { Clue.Create(Guid.NewGuid(), "Under the table", 0) };
            var subStage = TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find the Secret Key", 0, 100, "Find the key", "SECRET-123", clues);

            // Act & Assert
            subStage.Status.ActiveSessionCount.Should().Be(0);

            subStage.IncrementSessionCount();
            subStage.Status.ActiveSessionCount.Should().Be(1);

            subStage.DecrementSessionCount();
            subStage.Status.ActiveSessionCount.Should().Be(0);
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Store_Location_When_Provided()
        {
            // Act
            var subStage = TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null,
                latitude: 10.488158, longitude: -66.846534, searchRadiusMeters: 120);

            // Assert
            subStage.Latitude.Should().Be(10.488158);
            subStage.Longitude.Should().Be(-66.846534);
            subStage.SearchRadiusMeters.Should().Be(120);
        }

        [Fact]
        public void TreasureHuntSubStage_Without_Location_Should_Have_Null_Coordinates()
        {
            var subStage = TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null);

            subStage.Latitude.Should().BeNull();
            subStage.Longitude.Should().BeNull();
            subStage.SearchRadiusMeters.Should().BeNull();
        }

        [Theory]
        [InlineData(91.0, 0.0)]   // latitud fuera de rango
        [InlineData(-91.0, 0.0)]
        [InlineData(0.0, 181.0)]  // longitud fuera de rango
        [InlineData(0.0, -181.0)]
        public void TreasureHuntSubStage_Should_Throw_When_Coordinates_Out_Of_Range(double lat, double lon)
        {
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null,
                latitude: lat, longitude: lon);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void TreasureHuntSubStage_Should_Throw_When_Only_One_Coordinate_Provided()
        {
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null,
                latitude: 10.5, longitude: null);

            act.Should().Throw<ArgumentException>().WithMessage("*together*");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void TreasureHuntSubStage_Should_Throw_When_Radius_Not_Positive(int radius)
        {
            Action act = () => TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null,
                latitude: 10.5, longitude: -66.8, searchRadiusMeters: radius);

            act.Should().Throw<ArgumentException>().WithMessage("*radius*");
        }

        [Fact]
        public void UpdateDetails_WithoutLocation_Should_Preserve_Existing_Location()
        {
            // Arrange: subetapa con ubicación ya guardada.
            var subStage = TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null,
                latitude: 10.488158, longitude: -66.846534, searchRadiusMeters: 120);

            // Act: se actualiza solo el texto (sobrecarga de 2 args).
            subStage.UpdateDetails("Nuevo objetivo", "NUEVO-CODIGO");

            // Assert: el texto cambia pero la ubicación se conserva (antes se borraba).
            subStage.Objective.Should().Be("Nuevo objetivo");
            subStage.SecretCode.Should().Be("NUEVO-CODIGO");
            subStage.Latitude.Should().Be(10.488158);
            subStage.Longitude.Should().Be(-66.846534);
            subStage.SearchRadiusMeters.Should().Be(120);
        }

        [Fact]
        public void UpdateDetails_WithLocation_Should_Update_Location()
        {
            // Arrange: subetapa sin ubicación.
            var subStage = TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null);

            // Act: se actualiza con una ubicación nueva (sobrecarga completa).
            subStage.UpdateDetails("Objetivo", "SECRET-123",
                latitude: 40.416775, longitude: -3.703790, searchRadiusMeters: 250);

            // Assert.
            subStage.Latitude.Should().Be(40.416775);
            subStage.Longitude.Should().Be(-3.703790);
            subStage.SearchRadiusMeters.Should().Be(250);
        }

        [Fact]
        public void UpdateDetails_WithNullLocation_Should_Clear_Existing_Location()
        {
            // Arrange: subetapa con ubicación.
            var subStage = TreasureHuntSubStage.Create(
                Guid.NewGuid(), "Find it", 0, 100, "Objetivo", "SECRET-123", null,
                latitude: 10.488158, longitude: -66.846534, searchRadiusMeters: 120);

            // Act: la sobrecarga completa con nulls limpia la ubicación explícitamente.
            subStage.UpdateDetails("Objetivo", "SECRET-123",
                latitude: null, longitude: null, searchRadiusMeters: null);

            // Assert.
            subStage.Latitude.Should().BeNull();
            subStage.Longitude.Should().BeNull();
            subStage.SearchRadiusMeters.Should().BeNull();
        }
    }
}

