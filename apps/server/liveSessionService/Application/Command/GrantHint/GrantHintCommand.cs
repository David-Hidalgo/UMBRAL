using System;
using MediatR;

namespace LiveSessionService.Application.Commands
{
    // Iniciado por el operador: libera una pista concreta a un equipo.
    // Lleva el JWT del operador para leer el contenido de la pista desde el missionService.
    public sealed record GrantHintCommand(
        Guid SessionId,
        Guid TeamId,
        Guid SubStageId,
        Guid ClueId,
        string JwtToken
    ) : IRequest<Unit>;
}
