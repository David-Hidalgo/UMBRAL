using System;
using MediatR;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Queries
{
    /// <summary>Progreso de cada equipo en la etapa actual (RF-13), para guiar al operador.</summary>
    public sealed record GetStageProgressQuery(Guid SessionId) : IRequest<StageProgressDto>;
}
