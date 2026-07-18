using MediatR;

namespace UserService.Application.Command.DeleteUser;

public record DeleteUserCommand(string KeycloakUserId) : IRequest<Unit>;
