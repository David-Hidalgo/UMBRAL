using System;
using FluentAssertions;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.ValueObjects;
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
    }
}
