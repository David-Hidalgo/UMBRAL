using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using UserService.Application.Commands;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;

namespace UserService.Application.Handlers;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IIdentityProviderService _identityProvider;
    private readonly IEventBus _eventBus;

    public DeleteUserHandler(IIdentityProviderService identityProvider, IEventBus eventBus)
    {
        _identityProvider = identityProvider;
        _eventBus = eventBus;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _identityProvider.GetUserByIdAsync(request.KeycloakUserId, cancellationToken);
        if (user is null)
            throw new KeyNotFoundException($"User '{request.KeycloakUserId}' was not found.");

        if (!user.IsEnabled)
            throw new InvalidOperationException("User is already inactive.");

        await _identityProvider.SetUserEnabledAsync(request.KeycloakUserId, false, cancellationToken);
        await _eventBus.PublishAsync(new UserDeactivated(request.KeycloakUserId), cancellationToken);

        return Unit.Value;
    }
}
