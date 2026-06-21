using System.Threading;
using System.Threading.Tasks;

namespace MissionService.Application.Abstractions
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    }
}
