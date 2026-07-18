using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace shared.Logging;

/// <summary>
/// Intercepta cada invocacion de caso de uso, mide su duracion y delega
/// la auditoria (quien, que, resultado) en <see cref="IAuditLogger"/>.
/// No conoce el formato ni el destino de los logs: eso vive en el adaptador inyectado.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly string UseCase = typeof(TRequest).Name;

    private readonly IAuditLogger _auditLogger;

    public LoggingBehavior(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
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
            _auditLogger.LogSuccess(UseCase, ElapsedMilliseconds(startTimestamp));
            return response;
        }
        catch (Exception exception)
        {
            _auditLogger.LogFailure(UseCase, exception, ElapsedMilliseconds(startTimestamp));
            throw;
        }
    }

    private static long ElapsedMilliseconds(long startTimestamp)
        => (long)Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
}
