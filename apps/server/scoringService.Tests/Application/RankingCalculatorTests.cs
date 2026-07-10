using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using ScoringService.Application.Common;
using ScoringService.Domain.Entities;
using ScoringService.Domain.ValueObjects;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class RankingCalculatorTests
    {
        private static MonitoredTeam Team(Guid id, string code) => MonitoredTeam.Create(id, new TeamCode(code));

        private static ScoreEntry Evidence(Guid session, Guid team, int points, DateTime at)
            => ScoreEntry.ForEvidence(Guid.NewGuid(), session, team, ScoreValue.Award(points), Guid.NewGuid(), at);

        [Fact]
        public void Build_ShouldOrderByPointsDescending()
        {
            var session = Guid.NewGuid();
            var t1 = Guid.NewGuid();
            var t2 = Guid.NewGuid();
            var teams = new List<MonitoredTeam> { Team(t1, "Alfa"), Team(t2, "Beta") };
            var entries = new List<ScoreEntry>
            {
                Evidence(session, t1, 50, DateTime.UtcNow),
                Evidence(session, t2, 90, DateTime.UtcNow)
            };

            var ranking = RankingCalculator.Build(teams, entries);

            ranking[0].TeamId.Should().Be(t2);
            ranking[0].Rank.Should().Be(1);
            ranking[0].TotalPoints.Should().Be(90);
            ranking[1].TeamId.Should().Be(t1);
            ranking[1].Rank.Should().Be(2);
        }

        [Fact]
        public void Build_OnTie_ShouldRankEarlierResolutionFirst()
        {
            // RB-08: mismo puntaje, gana quien lo alcanzó antes.
            var session = Guid.NewGuid();
            var early = Guid.NewGuid();
            var late = Guid.NewGuid();
            var baseTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            var teams = new List<MonitoredTeam> { Team(late, "Tarde"), Team(early, "Temprano") };
            var entries = new List<ScoreEntry>
            {
                Evidence(session, late, 100, baseTime.AddMinutes(10)),
                Evidence(session, early, 100, baseTime.AddMinutes(2))
            };

            var ranking = RankingCalculator.Build(teams, entries);

            ranking[0].TeamId.Should().Be(early);
            ranking[1].TeamId.Should().Be(late);
        }

        [Fact]
        public void Build_ShouldIncludeTeamsWithoutPointsLast()
        {
            var session = Guid.NewGuid();
            var scored = Guid.NewGuid();
            var zero = Guid.NewGuid();
            var teams = new List<MonitoredTeam> { Team(scored, "ConPuntos"), Team(zero, "SinPuntos") };
            var entries = new List<ScoreEntry> { Evidence(session, scored, 10, DateTime.UtcNow) };

            var ranking = RankingCalculator.Build(teams, entries);

            ranking.Should().HaveCount(2);
            ranking.Last().TeamId.Should().Be(zero);
            ranking.Last().TotalPoints.Should().Be(0);
            ranking.Last().LastScoredAt.Should().BeNull();
        }

        [Fact]
        public void Build_ShouldSumMultipleEntriesPerTeam()
        {
            var session = Guid.NewGuid();
            var team = Guid.NewGuid();
            var teams = new List<MonitoredTeam> { Team(team, "Alfa") };
            var entries = new List<ScoreEntry>
            {
                Evidence(session, team, 40, DateTime.UtcNow),
                ScoreEntry.ForPenalty(Guid.NewGuid(), session, team, ScoreValue.Penalty(15), new PenaltyReason("x"), DateTime.UtcNow)
            };

            var ranking = RankingCalculator.Build(teams, entries);

            ranking.Single().TotalPoints.Should().Be(25);
        }
    }
}
