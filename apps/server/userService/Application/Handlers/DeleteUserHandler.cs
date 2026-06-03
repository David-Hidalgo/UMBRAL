using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Commands;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;
using UserService.Domain.RepositoryInterfaces;

namespace UserService.Application.Handlers;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserRepository _repository;
    private readonly IIdentityProviderService _identityProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<DeleteUserHandler> _logger;

    public DeleteUserHandler(
        IUserRepository repository,
        IIdentityProviderService identityProvider,
        IEventBus eventBus,
        ILogger<DeleteUserHandler> logger)
    {
        _repository = repository;
        _identityProvider = identityProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByKeycloakIdAsync(request.KeycloakUserId, cancellationToken);

        if (user is not null && !user.IsActive)
            throw new InvalidOperationException("User is already inactive.");

        // Keycloak is the source of truth — throws KeyNotFoundException if user doesn't exist
        await _identityProvider.SetUserEnabledAsync(request.KeycloakUserId, false, cancellationToken);

        // Local repo is best-effort (InMemory loses data on restart)
        if (user is not null)
        {
            user.Deactivate();

            try
            {
                await _repository.UpdateAsync(user, cancellationToken);
            }
            catch (Exception updateEx)
            {
                _logger.LogError(
                    updateEx,
                    "Failed to persist deactivation for user {KeycloakUserId}. Attempting Keycloak rollback.",
                    request.KeycloakUserId);

                try
                {
                    await _identityProvider.SetUserEnabledAsync(request.KeycloakUserId, true, cancellationToken);
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogCritical(
                        rollbackEx,
                        "Keycloak rollback failed for user {KeycloakUserId}. User is disabled in Keycloak but still active in local store.",
                        request.KeycloakUserId);
                }

                throw;
            }

            await _eventBus.PublishAsync(new UserDeactivated(user.Id, user.KeycloakUserId), cancellationToken);
        }

        return Unit.Value;
    }
}
