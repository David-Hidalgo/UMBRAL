using System;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    // Iniciado por un jugador: solicita una pista para el reto (subetapa) en el que está su equipo.
    public sealed record RequestHintCommand(
        Guid SessionId,
        Guid TeamId,
        Guid SubStageId,
        string KeycloakUserId
    ) : IRequest<Guid>;
}
