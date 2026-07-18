using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace shared.Tests.Logging;

/// <summary>
/// Doble de prueba que captura las entradas de log ya formateadas.
/// Se escribe a mano porque verificar ILogger.Log&lt;TState&gt; con Moq exige
/// matchers sobre It.IsAnyType que vuelven ilegibles las aserciones.
/// </summary>
public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        private NullScope() { }

        public void Dispose() { }
    }
}
