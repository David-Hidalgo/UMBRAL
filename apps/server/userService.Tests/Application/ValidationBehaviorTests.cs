using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using UserService.Application.Behaviors;
using UserService.Application.Commands;
using Xunit;

namespace UserService.Tests.Application;

public class ValidationBehaviorTests
{
    private static readonly CreateUserCommand ValidCommand =
        new("test@umbral.io", "Test User");

    // MediatR 14: RequestHandlerDelegate<TResponse> toma CancellationToken = default
    private static Task<Guid> NextDelegate(CancellationToken ct = default)
        => Task.FromResult(Guid.NewGuid());

    // ─── No validators ────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithNoValidators_CallsNextAndReturnsResult()
    {
        // Arrange
        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(
            Array.Empty<IValidator<CreateUserCommand>>());

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

    // ─── Validators pass ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithPassingValidator_CallsNextAndReturnsResult()
    {
        // Arrange
        var validatorMock = new Mock<IValidator<CreateUserCommand>>(MockBehavior.Strict);
        validatorMock
            .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateUserCommand>>()))
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(
            new[] { validatorMock.Object });

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
    public async Task Handle_WithMultiplePassingValidators_CallsNextOnce()
    {
        // Arrange
        var v1 = new Mock<IValidator<CreateUserCommand>>(MockBehavior.Strict);
        var v2 = new Mock<IValidator<CreateUserCommand>>(MockBehavior.Strict);

        v1.Setup(v => v.Validate(It.IsAny<ValidationContext<CreateUserCommand>>()))
          .Returns(new ValidationResult());
        v2.Setup(v => v.Validate(It.IsAny<ValidationContext<CreateUserCommand>>()))
          .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(
            new[] { v1.Object, v2.Object });

        var callCount = 0;

        Task<Guid> Next(CancellationToken ct = default)
        {
            callCount++;
            return Task.FromResult(Guid.NewGuid());
        }

        // Act
        await behavior.Handle(ValidCommand, Next, CancellationToken.None);

        // Assert
        callCount.Should().Be(1);
    }

    // ─── Validators fail ──────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithFailingValidator_ThrowsValidationException()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new(nameof(CreateUserCommand.Email), "Email is required.")
        };

        var validatorMock = new Mock<IValidator<CreateUserCommand>>(MockBehavior.Strict);
        validatorMock
            .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateUserCommand>>()))
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(
            new[] { validatorMock.Object });

        // Act
        var act = async () => await behavior.Handle(ValidCommand, NextDelegate, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_WithMultipleFailures_ThrowsExceptionContainingAllErrors()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new(nameof(CreateUserCommand.Email), "Email is required."),
            new(nameof(CreateUserCommand.Name), "Name is required.")
        };

        var validatorMock = new Mock<IValidator<CreateUserCommand>>(MockBehavior.Strict);
        validatorMock
            .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateUserCommand>>()))
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(
            new[] { validatorMock.Object });

        // Act
        var act = async () => await behavior.Handle(ValidCommand, NextDelegate, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithFailingValidator_DoesNotCallNextDelegate()
    {
        // Arrange
        var failures = new List<ValidationFailure>
        {
            new(nameof(CreateUserCommand.Email), "Email is required.")
        };

        var validatorMock = new Mock<IValidator<CreateUserCommand>>(MockBehavior.Strict);
        validatorMock
            .Setup(v => v.Validate(It.IsAny<ValidationContext<CreateUserCommand>>()))
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<CreateUserCommand, Guid>(
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
