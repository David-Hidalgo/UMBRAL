using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Common;

namespace LiveSessionService.Tests.Application
{
    public class ValidationBehaviorTests
    {
        private static readonly CreateTeamCommand ValidCommand = new(Guid.NewGuid(), "Equipo Alfa");

        private static Task<Guid> NextDelegate(CancellationToken ct = default)
            => Task.FromResult(Guid.NewGuid());

        [Fact]
        public async Task Handle_WithNoValidators_CallsNextAndReturnsResult()
        {
            // Arrange
            var behavior = new ValidationBehavior<CreateTeamCommand, Guid>(
                Array.Empty<IValidator<CreateTeamCommand>>());

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
            var validatorMock = new Mock<IValidator<CreateTeamCommand>>(MockBehavior.Strict);
            validatorMock
                .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateTeamCommand>>()))
                .Returns(new ValidationResult());

            var behavior = new ValidationBehavior<CreateTeamCommand, Guid>(
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
                new(nameof(CreateTeamCommand.TeamName), "El nombre del equipo es requerido.")
            };

            var validatorMock = new Mock<IValidator<CreateTeamCommand>>(MockBehavior.Strict);
            validatorMock
                .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateTeamCommand>>()))
                .Returns(new ValidationResult(failures));

            var behavior = new ValidationBehavior<CreateTeamCommand, Guid>(
                new[] { validatorMock.Object });

            // Act
            Func<Task> act = () => behavior.Handle(ValidCommand, NextDelegate, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_WithFailingValidator_DoesNotCallNextDelegate()
        {
            // Arrange
            var failures = new List<ValidationFailure>
            {
                new(nameof(CreateTeamCommand.TeamName), "El nombre del equipo es requerido.")
            };

            var validatorMock = new Mock<IValidator<CreateTeamCommand>>(MockBehavior.Strict);
            validatorMock
                .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateTeamCommand>>()))
                .Returns(new ValidationResult(failures));

            var behavior = new ValidationBehavior<CreateTeamCommand, Guid>(
                new[] { validatorMock.Object });

            var nextCalled = false;

            Task<Guid> Next(CancellationToken ct = default)
            {
                nextCalled = true;
                return Task.FromResult(Guid.NewGuid());
            }

            // Act
            Func<Task> act = () => behavior.Handle(ValidCommand, Next, CancellationToken.None);
            await act.Should().ThrowAsync<ValidationException>();

            // Assert
            nextCalled.Should().BeFalse();
        }
    }
}
