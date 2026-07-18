using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.IntegrationEvents;
using UserService.Application.Abstractions;

namespace UserService.Application.Command.CreateUser
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IEventBus _eventBus;
        private readonly IIdentityProviderService _identityProvider;

        public CreateUserHandler(IEventBus eventBus, IIdentityProviderService identityProvider)
        {
            _eventBus = eventBus;
            _identityProvider = identityProvider;
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var externalId = await _identityProvider.CreateUserAsync(
                request.Email, request.Name, request.Role, cancellationToken);

            var userId = Guid.NewGuid();
            await _eventBus.PublishAsync(
                new UserCreated(userId, externalId, request.Email.ToLowerInvariant(), request.Role),
                cancellationToken);

            return userId;
        }
    }
}
