using System.Threading;
using System.Threading.Tasks;

namespace MissionService.Application.Interfaces
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    }
}
