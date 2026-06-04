using MediatR;

namespace api.Application.Commands;

public record DeleteUserCommand(string KeycloakId) : IRequest<Unit>;
