using System;
using FluentAssertions;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.ValueObjects;
using ScoringService.Domain.Entities;
using Xunit;

namespace ScoringService.Tests.Domain
{
    public class ScoreValueTests
    {
        [Fact]
        public void Award_WithPositivePoints_ShouldStoreValue()
        {
            var value = ScoreValue.Award(100);
            value.Points.Should().Be(100);
        }

        [Fact]
        public void Award_WithNegativePoints_ShouldThrow()
        {
            Action act = () => ScoreValue.Award(-1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Penalty_WithPositivePoints_ShouldStoreNegativeDelta()
        {
            var value = ScoreValue.Penalty(30);
            value.Points.Should().Be(-30);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Penalty_WithNonPositivePoints_ShouldThrow(int points)
        {
            Action act = () => ScoreValue.Penalty(points);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Add_ShouldSumPoints()
        {
            var result = ScoreValue.Award(50).Add(ScoreValue.Penalty(20));
            result.Points.Should().Be(30);
        }

        [Fact]
        public void Zero_ShouldBeZero()
        {
            ScoreValue.Zero.Points.Should().Be(0);
        }
    }

    public class PenaltyReasonTests
    {
        [Fact]
        public void Constructor_WithText_ShouldTrimAndStore()
        {
            var reason = new PenaltyReason("  Uso indebido  ");
            reason.Value.Should().Be("Uso indebido");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Constructor_WithEmpty_ShouldThrowInvalidPenaltyReason(string? value)
        {
            Action act = () => new PenaltyReason(value!);
            act.Should().Throw<InvalidPenaltyReasonException>();
        }
    }

    public class TeamCodeTests
    {
        [Fact]
        public void Constructor_WithText_ShouldTrimAndStore()
        {
            var code = new TeamCode("  Alfa ");
            code.Value.Should().Be("Alfa");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Constructor_WithEmpty_ShouldThrow(string value)
        {
            Action act = () => new TeamCode(value);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ToString_ShouldReturnStringRepresentation()
        {
            var code = new TeamCode("Beta");
            code.ToString().Should().Be("Beta");
        }
    }

    public class MonitoredTeamTests
    {
        [Fact]
        public void Create_WithValidParameters_ShouldCreateInstance()
        {
            var teamId = Guid.NewGuid();
            var code = new TeamCode("Gamma");

            var team = MonitoredTeam.Create(teamId, code);

            team.TeamId.Should().Be(teamId);
            team.Code.Should().Be(code);
        }

        [Fact]
        public void Create_WithEmptyTeamId_ShouldThrow()
        {
            var code = new TeamCode("Gamma");
            Action act = () => MonitoredTeam.Create(Guid.Empty, code);
            act.Should().Throw<ArgumentException>().WithMessage("*identificador del equipo*");
        }

        [Fact]
        public void Create_WithNullCode_ShouldThrow()
        {
            Action act = () => MonitoredTeam.Create(Guid.NewGuid(), null!);
            act.Should().Throw<ArgumentNullException>();
        }
    }

    public static class ValueObjectAdditionalTests
    {
        [Fact]
        public static void ScoreValue_ToString_ShouldReturnPoints()
        {
            var val = ScoreValue.Award(42);
            val.ToString().Should().Be("42");
        }

        [Fact]
        public static void PenaltyReason_ToString_ShouldReturnReason()
        {
            var reason = new PenaltyReason("Infracción");
            reason.ToString().Should().Be("Infracción");
        }
    }
}
