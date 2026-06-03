using MediatR;

namespace UserService.Application.Commands;

public record DeleteUserCommand(string KeycloakUserId) : IRequest<Unit>;
