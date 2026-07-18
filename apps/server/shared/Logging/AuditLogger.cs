using System;
using Microsoft.Extensions.Logging;
using shared.Authentication;

namespace shared.Logging;

/// <summary>
/// Adaptador de <see cref="IAuditLogger"/> sobre el logger del framework.
/// Concentra el formato de las lineas de auditoria, la resolucion del invocador
/// y la politica de severidad. No persiste nada: la auditoria durable viaja por RabbitMQ.
/// Nunca registra el contenido del request para no filtrar datos sensibles.
/// </summary>
public sealed class AuditLogger : IAuditLogger
{
    private const string AnonymousUser = "anonymous";
    private const string NoRoles = "none";
    private const string RolesSeparator = ",";

    private const string SuccessTemplate =
        "Invocacion {UseCase} por {UserId} ({UserName}) roles=[{Roles}] resultado=Success duracion={ElapsedMs}ms";

    private const string FailureTemplate =
        "Invocacion {UseCase} por {UserId} ({UserName}) roles=[{Roles}] resultado=Failure {FailureType} duracion={ElapsedMs}ms";

    private readonly ILogger<AuditLogger> _logger;
    private readonly ICurrentUserService _currentUserService;

    public AuditLogger(
        ILogger<AuditLogger> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public void LogSuccess(string useCase, long elapsedMs)
    {
        _logger.LogInformation(
            SuccessTemplate,
            useCase,
            ResolveUserId(),
            ResolveUserName(),
            ResolveRoles(),
            elapsedMs);
    }

    public void LogFailure(string useCase, Exception exception, long elapsedMs)
    {
        _logger.Log(
            IsExpectedFailure(exception) ? LogLevel.Warning : LogLevel.Error,
            exception,
            FailureTemplate,
            useCase,
            ResolveUserId(),
            ResolveUserName(),
            ResolveRoles(),
            exception.GetType().Name,
            elapsedMs);
    }

    /// <summary>
    /// Los fallos esperados (entrada invalida, cancelacion) son Warning;
    /// el resto es Error porque delata un defecto del sistema.
    /// </summary>
    private static bool IsExpectedFailure(Exception exception)
        => exception is FluentValidation.ValidationException
            or ArgumentException
            or OperationCanceledException;

    private string ResolveUserId() => _currentUserService.UserId ?? AnonymousUser;

    private string ResolveUserName() => _currentUserService.UserName ?? AnonymousUser;

    private string ResolveRoles()
    {
        var roles = _currentUserService.Roles;
        return roles.Count == 0 ? NoRoles : string.Join(RolesSeparator, roles);
    }
}
