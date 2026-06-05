using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using api.Application.Commands;
using api.Application.Interfaces;

namespace api.Application.Handlers;

public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, Guid>
{
    private readonly IUserServiceClient _userServiceClient;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(IUserServiceClient userServiceClient, ILogger<CreateUserHandler> logger)
    {
        _userServiceClient = userServiceClient;
        _logger = logger;
    }

    public async Task<Guid> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating user via UserServiceClient");
        return await _userServiceClient.CreateUserAsync(request.Request, cancellationToken);
    }
}
