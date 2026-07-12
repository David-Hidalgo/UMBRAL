using System;
using MediatR;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Queries
{
    /// <summary>
    /// Reto actual de la sesión para el jugador, sin respuestas correctas (RF-06/08). Si se
    /// indica el equipo, cada subetapa incluye si ese equipo ya la resolvió (para mostrarlas
    /// de una en una).
    /// </summary>
    public sealed record GetCurrentChallengeQuery(Guid SessionId, Guid? TeamId = null)
        : IRequest<PlayerChallengeDto>;
}
