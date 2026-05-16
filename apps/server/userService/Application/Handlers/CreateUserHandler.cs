using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.Commands;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.RepositoryInterfaces;

namespace UserService.Application.Handlers
{
    public class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
    {
        private readonly IUserRepository _repository;
        private readonly IEventBus _eventBus;
        private readonly IIdentityProviderService _identityProvider;

        public CreateUserHandler(IUserRepository repository, IEventBus eventBus, IIdentityProviderService identityProvider)
        {
            _repository = repository;
            _eventBus = eventBus;
            _identityProvider = identityProvider;
        }

        public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var externalId = await _identityProvider.CreateUserAsync(request.Email, request.Name, request.Password, cancellationToken);
            var user = User.Create(Guid.NewGuid(), externalId, request.Email, request.Name);
            await _repository.AddAsync(user, cancellationToken);
            await _eventBus.PublishAsync(new UserCreated(user.Id, user.KeycloakUserId, user.Email.Value), cancellationToken);
            return user.Id;
        }
    }
}
