using System;
using System.Diagnostics;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    /// <summary>
    /// Detección de Docker para pruebas de integración, siguiendo el mismo patrón que
    /// missionService.Tests. Las pruebas de integración se omiten salvo que
    /// RUN_INTEGRATION_TESTS=true y Docker esté disponible.
    /// </summary>
    internal static class DockerHelper
    {
        private static readonly Lazy<bool> _isDockerAvailable = new(() =>
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "info",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(startInfo);
                if (process == null) return false;
                process.WaitForExit(2000);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        });

        public static bool IsDockerAvailable => _isDockerAvailable.Value;
    }

    public sealed class IntegrationFactAttribute : FactAttribute
    {
        public IntegrationFactAttribute()
        {
            if (Environment.GetEnvironmentVariable("RUN_INTEGRATION_TESTS") != "true")
            {
                Skip = "Pruebas de integración desactivadas. Usa RUN_INTEGRATION_TESTS=true para ejecutarlas.";
                return;
            }

            if (!DockerHelper.IsDockerAvailable)
            {
                Skip = "Docker no está disponible; esta prueba de integración lo requiere.";
            }
        }
    }
}
