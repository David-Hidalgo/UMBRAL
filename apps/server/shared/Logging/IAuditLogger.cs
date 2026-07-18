using System;

namespace shared.Logging;

/// <summary>
/// Puerto de auditoria de casos de uso. Abstrae el registro de "quien invoco
/// que, con que resultado y en cuanto tiempo" para que el <see cref="LoggingBehavior{TRequest,TResponse}"/>
/// no dependa directamente del logger del framework.
/// La implementacion concreta (adaptador) se inyecta por DI.
/// </summary>
public interface IAuditLogger
{
    /// <summary>Registra una invocacion que termino con exito.</summary>
    /// <param name="useCase">Nombre del caso de uso (tipo del request).</param>
    /// <param name="elapsedMs">Duracion de la invocacion en milisegundos.</param>
    void LogSuccess(string useCase, long elapsedMs);

    /// <summary>Registra una invocacion que termino con fallo.</summary>
    /// <param name="useCase">Nombre del caso de uso (tipo del request).</param>
    /// <param name="exception">Excepcion que provoco el fallo.</param>
    /// <param name="elapsedMs">Duracion de la invocacion en milisegundos.</param>
    void LogFailure(string useCase, Exception exception, long elapsedMs);
}
