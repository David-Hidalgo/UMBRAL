using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using shared.Authentication;
using shared.Logging;
using Xunit;

namespace shared.Tests.Logging;

public class LoggingBehaviorTests
{
    private const string UserId = "8f3a1b2c-0000-4444-9999-abcdefabcdef";
    private const string UserName = "operador1";

    /// <summary>Request con PII para fijar que el payload nunca se loguea.</summary>
    private sealed record CreateUserCommand(string Email, string Name);

    private static readonly CreateUserCommand SampleRequest = new("victima@ucab.edu.ve", "Juan Perez");

    private readonly RecordingLogger<LoggingBehavior<CreateUserCommand, Guid>> _logger = new();

    private static Mock<ICurrentUserService> AuthenticatedUser(params string[] roles)
    {
        var mock = new Mock<ICurrentUserService>(MockBehavior.Strict);
        mock.SetupGet(s => s.UserId).Returns(UserId);
        mock.SetupGet(s => s.UserName).Returns(UserName);
        mock.SetupGet(s => s.Roles).Returns(roles);
        return mock;
    }

    private static Mock<ICurrentUserService> AnonymousUser()
    {
        var mock = new Mock<ICurrentUserService>(MockBehavior.Strict);
        mock.SetupGet(s => s.UserId).Returns((string?)null);
        mock.SetupGet(s => s.UserName).Returns((string?)null);
        mock.SetupGet(s => s.Roles).Returns(Array.Empty<string>());
        return mock;
    }

    private LoggingBehavior<CreateUserCommand, Guid> CreateBehavior(ICurrentUserService currentUser)
        => new(_logger, currentUser);

    [Fact]
    public async Task Handle_WhenHandlerSucceeds_CallsNextAndReturnsResponse()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);
        var expectedId = Guid.NewGuid();
        var nextCalled = false;

        Task<Guid> Next(CancellationToken ct = default)
        {
            nextCalled = true;
            return Task.FromResult(expectedId);
        }

        // Act
        var result = await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task Handle_WhenHandlerSucceeds_LogsInformationOnce()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Information);

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenHandlerSucceeds_LogsUseCaseNameUserIdAndRoles()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator", "admin").Object);

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        var message = _logger.Entries.Should().ContainSingle().Subject.Message;
        message.Should().Contain(nameof(CreateUserCommand));
        message.Should().Contain(UserId);
        message.Should().Contain(UserName);
        message.Should().Contain("roles=[operator,admin]");
        message.Should().Contain("resultado=Success");

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenHandlerSucceeds_LogsElapsedMilliseconds()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().MatchRegex(@"duracion=\d+ms");

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenRequestContainsPii_DoesNotLogRequestPayload()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        var message = _logger.Entries.Should().ContainSingle().Subject.Message;
        message.Should().NotContain(SampleRequest.Email);
        message.Should().NotContain(SampleRequest.Name);

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenUserIsAnonymous_LogsAnonymousPlaceholder()
    {
        // Arrange
        var behavior = CreateBehavior(AnonymousUser().Object);

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("por anonymous (anonymous)");

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenUserHasNoRoles_LogsNonePlaceholder()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser().Object);

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("roles=[none]");

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenHandlerThrowsUnexpectedException_LogsError()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);
        var expected = new InvalidOperationException("boom");

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeSameAs(expected);

        Task<Guid> Next(CancellationToken ct = default) => throw expected;
    }

    [Fact]
    public async Task Handle_WhenHandlerThrowsUnexpectedException_RethrowsSameException()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);
        var expected = new InvalidOperationException("boom");

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(expected);

        Task<Guid> Next(CancellationToken ct = default) => throw expected;
    }

    [Fact]
    public async Task Handle_WhenHandlerThrowsValidationException_LogsWarning()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);
        var failures = new List<ValidationFailure> { new("Email", "Email requerido") };

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _logger.Entries.Should().ContainSingle()
            .Subject.Level.Should().Be(LogLevel.Warning);

        Task<Guid> Next(CancellationToken ct = default) => throw new ValidationException(failures);
    }

    [Fact]
    public async Task Handle_WhenHandlerThrowsArgumentException_LogsWarning()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _logger.Entries.Should().ContainSingle()
            .Subject.Level.Should().Be(LogLevel.Warning);

        static Task<Guid> Next(CancellationToken ct = default) => throw new ArgumentException("invalido");
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_LogsExceptionTypeName()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("resultado=Failure InvalidOperationException");

        static Task<Guid> Next(CancellationToken ct = default) => throw new InvalidOperationException("boom");
    }

    [Fact]
    public async Task Handle_Always_PassesCancellationTokenToNext()
    {
        // Arrange
        var behavior = CreateBehavior(AuthenticatedUser("operator").Object);
        using var cts = new CancellationTokenSource();
        var received = CancellationToken.None;

        Task<Guid> Next(CancellationToken ct = default)
        {
            received = ct;
            return Task.FromResult(Guid.NewGuid());
        }

        // Act
        await behavior.Handle(SampleRequest, Next, cts.Token);

        // Assert
        received.Should().Be(cts.Token);
    }
}
