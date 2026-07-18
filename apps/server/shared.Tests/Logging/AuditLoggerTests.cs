using System;
using System.Collections.Generic;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using shared.Authentication;
using shared.Logging;
using Xunit;

namespace shared.Tests.Logging;

public class AuditLoggerTests
{
    private const string UserId = "8f3a1b2c-0000-4444-9999-abcdefabcdef";
    private const string UserName = "operador1";
    private const string UseCase = "CreateUserCommand";

    private readonly RecordingLogger<AuditLogger> _logger = new();

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

    private AuditLogger CreateLogger(ICurrentUserService currentUser) => new(_logger, currentUser);

    [Fact]
    public void LogSuccess_LogsInformationOnce()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator").Object);

        // Act
        auditLogger.LogSuccess(UseCase, 12);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Which.Level.Should().Be(LogLevel.Information);
    }

    [Fact]
    public void LogSuccess_LogsUseCaseNameUserIdRolesAndResult()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator", "admin").Object);

        // Act
        auditLogger.LogSuccess(UseCase, 12);

        // Assert
        var message = _logger.Entries.Should().ContainSingle().Subject.Message;
        message.Should().Contain(UseCase);
        message.Should().Contain(UserId);
        message.Should().Contain(UserName);
        message.Should().Contain("roles=[operator,admin]");
        message.Should().Contain("resultado=Success");
    }

    [Fact]
    public void LogSuccess_LogsElapsedMilliseconds()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator").Object);

        // Act
        auditLogger.LogSuccess(UseCase, 42);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("duracion=42ms");
    }

    [Fact]
    public void LogSuccess_WhenUserIsAnonymous_LogsAnonymousPlaceholder()
    {
        // Arrange
        var auditLogger = CreateLogger(AnonymousUser().Object);

        // Act
        auditLogger.LogSuccess(UseCase, 1);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("por anonymous (anonymous)");
    }

    [Fact]
    public void LogSuccess_WhenUserHasNoRoles_LogsNonePlaceholder()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser().Object);

        // Act
        auditLogger.LogSuccess(UseCase, 1);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("roles=[none]");
    }

    [Fact]
    public void LogFailure_WhenUnexpectedException_LogsErrorWithSameException()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator").Object);
        var expected = new InvalidOperationException("boom");

        // Act
        auditLogger.LogFailure(UseCase, expected, 5);

        // Assert
        var entry = _logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Error);
        entry.Exception.Should().BeSameAs(expected);
    }

    [Fact]
    public void LogFailure_WhenValidationException_LogsWarning()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator").Object);
        var failures = new List<ValidationFailure> { new("Email", "Email requerido") };

        // Act
        auditLogger.LogFailure(UseCase, new ValidationException(failures), 5);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void LogFailure_WhenArgumentException_LogsWarning()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator").Object);

        // Act
        auditLogger.LogFailure(UseCase, new ArgumentException("invalido"), 5);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Level.Should().Be(LogLevel.Warning);
    }

    [Fact]
    public void LogFailure_LogsExceptionTypeNameAndFailureResult()
    {
        // Arrange
        var auditLogger = CreateLogger(AuthenticatedUser("operator").Object);

        // Act
        auditLogger.LogFailure(UseCase, new InvalidOperationException("boom"), 5);

        // Assert
        _logger.Entries.Should().ContainSingle()
            .Subject.Message.Should().Contain("resultado=Failure InvalidOperationException");
    }
}
