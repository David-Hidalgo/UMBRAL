using System;
using System.Collections.Generic;
using FluentAssertions;
using MissionService.Application.Commands;
using MissionService.Application.Validators;
using Xunit;

namespace MissionService.Tests.Application
{
    public class ReorderStagesCommandValidatorTests
    {
        private readonly ReorderStagesCommandValidator _validator = new();

        [Fact]
        public void Should_Be_Valid_When_All_Fields_Are_Correct()
        {
            // Arrange
            var command = new ReorderStagesCommand(
                Guid.NewGuid(),
                new List<Guid> { Guid.NewGuid(), Guid.NewGuid() });

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void Should_Be_Invalid_When_MissionId_Is_Empty()
        {
            // Arrange
            var command = new ReorderStagesCommand(
                Guid.Empty,
                new List<Guid> { Guid.NewGuid() });

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "MissionId");
        }

        [Fact]
        public void Should_Be_Invalid_When_StageIdsInOrder_Is_Empty()
        {
            // Arrange
            var command = new ReorderStagesCommand(Guid.NewGuid(), new List<Guid>());

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "StageIdsInOrder");
        }

        [Fact]
        public void Should_Be_Invalid_When_StageIdsInOrder_Contains_Empty_Guid()
        {
            // Arrange
            var command = new ReorderStagesCommand(
                Guid.NewGuid(),
                new List<Guid> { Guid.NewGuid(), Guid.Empty });

            // Act
            var result = _validator.Validate(command);

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "StageIdsInOrder");
        }
    }
}
