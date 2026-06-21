using System.Threading;
using System.Threading.Tasks;

namespace LiveSessionService.Application.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    }
}
