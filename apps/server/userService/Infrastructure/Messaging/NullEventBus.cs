using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UserService.Application.Interfaces;

namespace UserService.Infrastructure.Messaging
{
    public class NullEventBus : IEventBus
    {
        public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default)
        {
            Console.WriteLine($"[NullEventBus] Event published: {typeof(T).Name} - {JsonSerializer.Serialize(@event)}");
            return Task.CompletedTask;
        }
    }
}
