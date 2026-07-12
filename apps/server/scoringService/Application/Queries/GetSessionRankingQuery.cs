using System;
using System.Collections.Generic;
using MediatR;
using ScoringService.Application.DTOs;

namespace ScoringService.Application.Queries
{
    /// <summary>Ranking actual de la sesión, ordenado por RB-08 (RF-12).</summary>
    public sealed record GetSessionRankingQuery(Guid SessionId) : IRequest<IReadOnlyList<RankingEntryDto>>;
}
