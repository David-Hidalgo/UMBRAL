using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using UserService.Application.Commands;
using UserService.Application.IntegrationEvents;
using UserService.Application.Interfaces;

namespace UserService.Application.Handlers
{
    public class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Unit>
    {
        private readonly IEventBus _eventBus;
        private readonly IIdentityProviderService _identityProvider;
        private readonly ILogger<UpdateUserHandler> _logger;

        public UpdateUserHandler(
            IEventBus eventBus,
            IIdentityProviderService identityProvider,
            ILogger<UpdateUserHandler> logger)
        {
            _eventBus = eventBus;
            _identityProvider = identityProvider;
            _logger = logger;
        }

        public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var keycloakUserId = request.Id.ToString();

            // 1. Snapshot desde Keycloak (fuente de verdad + verificacion de existencia / 404).
            var current = await _identityProvider.GetUserByIdAsync(keycloakUserId, cancellationToken);
            if (current is null)
                throw new KeyNotFoundException($"User '{keycloakUserId}' was not found.");

            // 2. Actualizar nombre en Keycloak.
            await _identityProvider.UpdateUserAsync(keycloakUserId, request.Name, cancellationToken);

            // 3. Actualizar rol con compensacion distribuida: si falla, restaura el nombre previo.
            try
            {
                await _identityProvider.UpdateUserRoleAsync(keycloakUserId, request.Role, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Role update failed for Keycloak user {KeycloakUserId}. Compensating by restoring previous name '{PreviousName}'.",
                    keycloakUserId, current.Name);
                await _identityProvider.UpdateUserAsync(keycloakUserId, current.Name, cancellationToken);
                throw;
            }

            // 4. Evento de integracion para desacoplar auditoria / sincronizacion (RabbitMQ).
            await _eventBus.PublishAsync(
                new UserUpdated(request.Id, keycloakUserId, request.Name, request.Role),
                cancellationToken);

            return Unit.Value;
        }
    }
}
