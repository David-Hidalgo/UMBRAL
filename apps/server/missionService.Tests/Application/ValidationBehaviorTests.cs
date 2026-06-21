using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using MissionService.Application.Common;
using MissionService.Application.Commands;
using Moq;
using Xunit;

namespace MissionService.Tests.Application
{
    public class ValidationBehaviorTests
    {
        private static readonly CreateMissionCommand ValidCommand = new(
            "Mission Name",
            "Mission Description",
            "Beginner",
            60,
            new List<CreateStageCommand>()
        );

        private static Task<Guid> NextDelegate(CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid());

        [Fact]
        public async Task Handle_WithNoValidators_CallsNextAndReturnsResult()
        {
            // Arrange
            var behavior = new ValidationBehavior<CreateMissionCommand, Guid>(
                Array.Empty<IValidator<CreateMissionCommand>>());

            var expectedId = Guid.NewGuid();
            var nextCalled = false;

            Task<Guid> Next(CancellationToken ct = default)
            {
                nextCalled = true;
                return Task.FromResult(expectedId);
            }

            // Act
            var result = await behavior.Handle(ValidCommand, Next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
            result.Should().Be(expectedId);
        }

        [Fact]
        public async Task Handle_WithPassingValidator_CallsNextAndReturnsResult()
        {
            // Arrange
            var validatorMock = new Mock<IValidator<CreateMissionCommand>>(MockBehavior.Strict);
            validatorMock
                .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateMissionCommand>>()))
                .Returns(new ValidationResult());

            var behavior = new ValidationBehavior<CreateMissionCommand, Guid>(
                new[] { validatorMock.Object });

            var nextCalled = false;

            Task<Guid> Next(CancellationToken ct = default)
            {
                nextCalled = true;
                return Task.FromResult(Guid.NewGuid());
            }

            // Act
            await behavior.Handle(ValidCommand, Next, CancellationToken.None);

            // Assert
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithFailingValidator_ThrowsValidationException()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new("Name", "Name is required.")
            };

            var validatorMock = new Mock<IValidator<CreateMissionCommand>>(MockBehavior.Strict);
            validatorMock
                .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateMissionCommand>>()))
                .Returns(new ValidationResult(failures));

            var behavior = new ValidationBehavior<CreateMissionCommand, Guid>(
                new[] { validatorMock.Object });

            // Act
            var act = async () => await behavior.Handle(ValidCommand, NextDelegate, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_WithFailingValidator_DoesNotCallNextDelegate()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new("Name", "Name is required.")
            };

            var validatorMock = new Mock<IValidator<CreateMissionCommand>>(MockBehavior.Strict);
            validatorMock
                .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateMissionCommand>>()))
                .Returns(new ValidationResult(failures));

            var behavior = new ValidationBehavior<CreateMissionCommand, Guid>(
                new[] { validatorMock.Object });

            var nextCalled = false;

            Task<Guid> Next(CancellationToken ct = default)
            {
                nextCalled = true;
                return Task.FromResult(Guid.NewGuid());
            }

            // Act
            var act = async () => await behavior.Handle(ValidCommand, Next, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            nextCalled.Should().BeFalse();
        }
    }
}
