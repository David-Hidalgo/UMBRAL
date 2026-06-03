using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using api.Application.Commands;
using api.Application.Interfaces;

namespace api.Application.Handlers;

public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Unit>
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<UpdateUserHandler> _logger;

    public UpdateUserHandler(IUserServiceClient userServiceClient, ILogger<UpdateUserHandler> logger)
    {
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<Unit> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Updating user {UserId} via UserServiceClient", request.Id);
        await _userServiceClient.UpdateUserAsync(request.Id, request.Request, cancellationToken);
        return Unit.Value;
    }
}
