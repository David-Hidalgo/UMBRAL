using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Application.Abstractions
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    }
}
