using System;
using System.Collections.Generic;
using System.Linq;
using ScoringService.Application.DTOs;
using ScoringService.Domain.Entities;

namespace ScoringService.Application.Common
{
    /// <summary>
    /// Fuente única de la regla de ordenamiento del ranking (RB-08): mayor puntaje primero y, en
    /// caso de empate, gana el equipo que alcanzó ese puntaje antes (menor "tiempo de resolución",
    /// aproximado por el instante de su último asiento de puntaje). Los equipos sin puntaje van al
    /// final. Se incluyen todos los equipos de la sesión, aunque tengan cero puntos.
    /// </summary>
    public static class RankingCalculator
    {
        public static IReadOnlyList<RankingEntryDto> Build(
            IReadOnlyCollection<MonitoredTeam> teams,
            IReadOnlyList<ScoreEntry> entries)
        {
            var byTeam = entries
                .GroupBy(e => e.TeamId)
                .ToDictionary(
                    g => g.Key,
                    g => new
                    {
                        Total = g.Sum(e => e.Value.Points),
                        LastScoredAt = (DateTime?)g.Max(e => e.OccurredAt)
                    });

            var ordered = teams
                .Select(t =>
                {
                    byTeam.TryGetValue(t.TeamId, out var agg);
                    return new
                    {
                        t.TeamId,
                        TeamCode = t.Code.Value,
                        Total = agg?.Total ?? 0,
                        LastScoredAt = agg?.LastScoredAt
                    };
                })
                .OrderByDescending(x => x.Total)
                .ThenBy(x => x.LastScoredAt ?? DateTime.MaxValue)
                .ToList();

            return ordered
                .Select((x, index) => new RankingEntryDto(
                    Rank: index + 1,
                    TeamId: x.TeamId,
                    TeamCode: x.TeamCode,
                    TotalPoints: x.Total,
                    LastScoredAt: x.LastScoredAt))
                .ToList();
        }
    }
}
