using System;
using MediatR;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Queries
{
    /// <summary>Puntaje actual de un equipo para la app móvil (RF-06).</summary>
    public sealed record GetTeamScoreQuery(Guid SessionId, Guid TeamId) : IRequest<TeamScoreDto>;
}
