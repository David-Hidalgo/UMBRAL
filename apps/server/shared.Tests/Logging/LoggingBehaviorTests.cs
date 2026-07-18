using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using shared.Logging;
using Xunit;

namespace shared.Tests.Logging;

public class LoggingBehaviorTests
{
    /// <summary>Request con PII para fijar que el behavior nunca reenvia el payload al auditor.</summary>
    private sealed record CreateUserCommand(string Email, string Name);

    private static readonly CreateUserCommand SampleRequest = new("victima@ucab.edu.ve", "Juan Perez");

    private readonly RecordingAuditLogger _auditLogger = new();

    private LoggingBehavior<CreateUserCommand, Guid> CreateBehavior() => new(_auditLogger);

    [Fact]
    public async Task Handle_WhenHandlerSucceeds_CallsNextAndReturnsResponse()
    {
        // Arrange
        var behavior = CreateBehavior();
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
    public async Task Handle_WhenHandlerSucceeds_AuditsSuccessOnceWithUseCaseName()
    {
        // Arrange
        var behavior = CreateBehavior();

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        var entry = _auditLogger.Entries.Should().ContainSingle().Subject;
        entry.Succeeded.Should().BeTrue();
        entry.UseCase.Should().Be(nameof(CreateUserCommand));
        entry.Exception.Should().BeNull();

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenHandlerSucceeds_AuditsElapsedMilliseconds()
    {
        // Arrange
        var behavior = CreateBehavior();

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        _auditLogger.Entries.Should().ContainSingle()
            .Subject.ElapsedMs.Should().BeGreaterThanOrEqualTo(0);

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_Always_DoesNotForwardRequestPayloadToAuditor()
    {
        // Arrange — el behavior solo conoce el nombre del caso de uso, nunca el payload.
        var behavior = CreateBehavior();

        // Act
        await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        var entry = _auditLogger.Entries.Should().ContainSingle().Subject;
        entry.UseCase.Should().NotContain(SampleRequest.Email);
        entry.UseCase.Should().NotContain(SampleRequest.Name);

        static Task<Guid> Next(CancellationToken ct = default) => Task.FromResult(Guid.NewGuid());
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_AuditsFailureWithSameException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var expected = new InvalidOperationException("boom");

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        var entry = _auditLogger.Entries.Should().ContainSingle().Subject;
        entry.Succeeded.Should().BeFalse();
        entry.UseCase.Should().Be(nameof(CreateUserCommand));
        entry.Exception.Should().BeSameAs(expected);

        Task<Guid> Next(CancellationToken ct = default) => throw expected;
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_RethrowsSameException()
    {
        // Arrange
        var behavior = CreateBehavior();
        var expected = new InvalidOperationException("boom");

        // Act
        var act = async () => await behavior.Handle(SampleRequest, Next, CancellationToken.None);

        // Assert
        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(expected);

        Task<Guid> Next(CancellationToken ct = default) => throw expected;
    }

    [Fact]
    public async Task Handle_Always_PassesCancellationTokenToNext()
    {
        // Arrange
        var behavior = CreateBehavior();
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

    /// <summary>Doble de prueba de <see cref="IAuditLogger"/> que captura cada invocacion auditada.</summary>
    private sealed class RecordingAuditLogger : IAuditLogger
    {
        private readonly List<AuditEntry> _entries = new();

        public IReadOnlyList<AuditEntry> Entries => _entries;

        public void LogSuccess(string useCase, long elapsedMs)
            => _entries.Add(new AuditEntry(useCase, true, null, elapsedMs));

        public void LogFailure(string useCase, Exception exception, long elapsedMs)
            => _entries.Add(new AuditEntry(useCase, false, exception, elapsedMs));

        public sealed record AuditEntry(string UseCase, bool Succeeded, Exception? Exception, long ElapsedMs);
    }
}
