using System.Threading.Tasks;

namespace UserService.Application.Abstractions
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default);
    }
}
