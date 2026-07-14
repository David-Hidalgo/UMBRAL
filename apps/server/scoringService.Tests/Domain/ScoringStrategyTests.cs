using System;
using FluentAssertions;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Scoring;
using Xunit;

namespace ScoringService.Tests.Domain
{
    public class ScoringStrategyTests
    {
        [Fact]
        public void Beginner_ShouldAwardBasePointsUnchanged()
        {
            var strategy = new BeginnerScoringStrategy();

            strategy.Difficulty.Should().Be(MissionDifficulty.Beginner);
            strategy.CalculateAward(100).Points.Should().Be(100);
        }

        [Fact]
        public void Intermediate_ShouldAddFiftyPercent()
        {
            var strategy = new IntermediateScoringStrategy();

            strategy.Difficulty.Should().Be(MissionDifficulty.Intermediate);
            strategy.CalculateAward(100).Points.Should().Be(150);
        }

        [Fact]
        public void Advanced_ShouldDoublePoints()
        {
            var strategy = new AdvancedScoringStrategy();

            strategy.Difficulty.Should().Be(MissionDifficulty.Advanced);
            strategy.CalculateAward(50).Points.Should().Be(100);
        }

        [Theory]
        [InlineData(75, 113)]  // 75 × 1.5 = 112.5 → 113 (medios hacia arriba)
        [InlineData(45, 68)]   // 45 × 1.5 = 67.5  → 68
        [InlineData(0, 0)]
        public void Intermediate_ShouldRoundHalfAwayFromZero(int basePoints, int expected)
        {
            new IntermediateScoringStrategy().CalculateAward(basePoints).Points.Should().Be(expected);
        }

        [Fact]
        public void CalculateAward_WithNegativeBase_ShouldThrow()
        {
            Action act = () => new BeginnerScoringStrategy().CalculateAward(-1);

            act.Should().Throw<ArgumentException>();
        }
    }

    public class ScoringStrategyResolverTests
    {
        private static ScoringStrategyResolver BuildResolver() => new(
            new IScoringStrategy[]
            {
                new BeginnerScoringStrategy(),
                new IntermediateScoringStrategy(),
                new AdvancedScoringStrategy()
            });

        [Theory]
        [InlineData(MissionDifficulty.Beginner, 100)]
        [InlineData(MissionDifficulty.Intermediate, 150)]
        [InlineData(MissionDifficulty.Advanced, 200)]
        public void Resolve_ShouldReturnStrategyForDifficulty(MissionDifficulty difficulty, int expected)
        {
            var strategy = BuildResolver().Resolve(difficulty);

            strategy.Difficulty.Should().Be(difficulty);
            strategy.CalculateAward(100).Points.Should().Be(expected);
        }

        [Fact]
        public void Resolve_WhenStrategyMissing_ShouldFallBackToBeginner()
        {
            // Solo se registran Intermedia/Avanzada: al pedir una dificultad sin estrategia,
            // el resolver cae en Principiante (×1.0) para nunca dejar sin puntuar.
            var resolver = new ScoringStrategyResolver(
                new IScoringStrategy[] { new IntermediateScoringStrategy(), new AdvancedScoringStrategy() });

            var strategy = resolver.Resolve(MissionDifficulty.Beginner);

            strategy.CalculateAward(100).Points.Should().Be(100);
        }
    }

    public class MissionDifficultyExtensionsTests
    {
        [Theory]
        [InlineData("Beginner", MissionDifficulty.Beginner)]
        [InlineData("intermediate", MissionDifficulty.Intermediate)]
        [InlineData("  Advanced ", MissionDifficulty.Advanced)]
        public void FromString_ShouldParseKnownValues(string value, MissionDifficulty expected)
        {
            MissionDifficultyExtensions.FromString(value).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Legendary")]
        public void FromString_WithMissingOrUnknown_ShouldDefaultToBeginner(string? value)
        {
            MissionDifficultyExtensions.FromString(value).Should().Be(MissionDifficulty.Beginner);
        }
    }
}
