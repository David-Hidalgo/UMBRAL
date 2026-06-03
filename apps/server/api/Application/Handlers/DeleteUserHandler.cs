using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using api.Application.Commands;
using api.Application.Interfaces;

namespace api.Application.Handlers;

public sealed class DeleteUserHandler : IRequestHandler<DeleteUserCommand, Unit>
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<DeleteUserHandler> _logger;

    public DeleteUserHandler(IUserServiceClient userServiceClient, ILogger<DeleteUserHandler> logger)
    {
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deleting user {KeycloakId} via UserServiceClient", request.KeycloakId);
        await _userServiceClient.DeleteUserAsync(request.KeycloakId, cancellationToken);
        return Unit.Value;
    }
}
