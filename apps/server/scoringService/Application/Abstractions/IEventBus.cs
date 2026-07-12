using System.Threading;
using System.Threading.Tasks;

namespace ScoringService.Application.Abstractions
{
    /// <summary>Puerto de publicación de eventos de integración (RabbitMQ) — RF-14.</summary>
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    }
}
