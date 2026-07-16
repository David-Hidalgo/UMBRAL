using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using shared.Authentication;

namespace shared.Logging;

/// <summary>
/// Audita cada invocacion de caso de uso: quien, que, resultado y duracion.
/// No persiste nada: la auditoria durable viaja por RabbitMQ.
/// Nunca registra el contenido del request para no filtrar datos sensibles.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const string AnonymousUser = "anonymous";
    private const string NoRoles = "none";
    private const string RolesSeparator = ",";

    private const string SuccessTemplate =
        "Invocacion {UseCase} por {UserId} ({UserName}) roles=[{Roles}] resultado=Success duracion={ElapsedMs}ms";

    private const string FailureTemplate =
        "Invocacion {UseCase} por {UserId} ({UserName}) roles=[{Roles}] resultado=Failure {FailureType} duracion={ElapsedMs}ms";

    private static readonly string UseCase = typeof(TRequest).Name;

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var startTimestamp = Stopwatch.GetTimestamp();

        try
        {
            var response = await next(cancellationToken);
            LogSuccess(startTimestamp);
            return response;
        }
        catch (Exception exception)
        {
            LogFailure(exception, startTimestamp);
            throw;
        }
    }

    private void LogSuccess(long startTimestamp)
    {
        _logger.LogInformation(
            SuccessTemplate,
            UseCase,
            ResolveUserId(),
            ResolveUserName(),
            ResolveRoles(),
            ElapsedMilliseconds(startTimestamp));
    }

    private void LogFailure(Exception exception, long startTimestamp)
    {
        _logger.Log(
            IsExpectedFailure(exception) ? LogLevel.Warning : LogLevel.Error,
            exception,
            FailureTemplate,
            UseCase,
            ResolveUserId(),
            ResolveUserName(),
            ResolveRoles(),
            exception.GetType().Name,
            ElapsedMilliseconds(startTimestamp));
    }

    /// <summary>
    /// Los fallos esperados (entrada invalida, cancelacion) son Warning;
    /// el resto es Error porque delata un defecto del sistema.
    /// </summary>
    private static bool IsExpectedFailure(Exception exception)
        => exception is FluentValidation.ValidationException
            or ArgumentException
            or OperationCanceledException;

    private static long ElapsedMilliseconds(long startTimestamp)
        => (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;

    private string ResolveUserId() => _currentUserService.UserId ?? AnonymousUser;

    private string ResolveUserName() => _currentUserService.UserName ?? AnonymousUser;

    private string ResolveRoles()
    {
        var roles = _currentUserService.Roles;
        return roles.Count == 0 ? NoRoles : string.Join(RolesSeparator, roles);
    }
}
