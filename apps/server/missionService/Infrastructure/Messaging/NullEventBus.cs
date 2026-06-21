using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MissionService.Application.Abstractions;

namespace MissionService.Infrastructure.Messaging
{
    public sealed class NullEventBus : IEventBus
    {
        private readonly ILogger<NullEventBus> _logger;

        public NullEventBus(ILogger<NullEventBus> logger)
        {
            _logger = logger;
        }

        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("[NullEventBus] Event published: {EventType}", typeof(T).Name);
            }

            return Task.CompletedTask;
        }
    }
}
