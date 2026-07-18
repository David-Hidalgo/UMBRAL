using System;
using FluentAssertions;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.ValueObjects;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class StatusAndEnumsTests
    {
        [Fact]
        public void Status_Should_Initialize_To_Zero_By_Default()
        {
            var status = new Status();
            status.ActiveSessionCount.Should().Be(0);
            status.IsInUse.Should().BeFalse();
        }

        [Fact]
        public void Status_Should_Initialize_To_Custom_Value()
        {
            var status = new Status(5);
            status.ActiveSessionCount.Should().Be(5);
            status.IsInUse.Should().BeTrue();
        }

        [Fact]
        public void Status_Should_Throw_ArgumentException_For_Negative_Count()
        {
            Action act = () => new Status(-1);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Status_Increment_Should_Increase_Count()
        {
            var status = new Status(0);
            var updated = status.Increment();

            updated.ActiveSessionCount.Should().Be(1);
            updated.IsInUse.Should().BeTrue();

            // Original status should remain unchanged (immutable VO)
            status.ActiveSessionCount.Should().Be(0);
        }

        [Fact]
        public void Status_Decrement_Should_Decrease_Count()
        {
            var status = new Status(2);
            var updated = status.Decrement();

            updated.ActiveSessionCount.Should().Be(1);

            // Original status should remain unchanged (immutable VO)
            status.ActiveSessionCount.Should().Be(2);
        }

        [Fact]
        public void Status_Decrement_Should_Throw_InvalidOperationException_When_Count_Is_Zero()
        {
            var status = new Status(0);
            Action act = () => status.Decrement();
            act.Should().Throw<InvalidOperationException>();
        }

        [Theory]
        [InlineData("Beginner")]
        [InlineData("beginner")]
        [InlineData(" BEGINNER ")]
        public void DifficultyLevel_FromString_Should_Parse_Beginner(string input)
        {
            var level = DifficultyLevelExtensions.FromString(input);
            level.Should().Be(DifficultyLevel.Beginner);
        }

        [Theory]
        [InlineData("Intermediate")]
        [InlineData("intermediate")]
        public void DifficultyLevel_FromString_Should_Parse_Intermediate(string input)
        {
            var level = DifficultyLevelExtensions.FromString(input);
            level.Should().Be(DifficultyLevel.Intermediate);
        }

        [Theory]
        [InlineData("Advanced")]
        [InlineData("advanced")]
        public void DifficultyLevel_FromString_Should_Parse_Advanced(string input)
        {
            var level = DifficultyLevelExtensions.FromString(input);
            level.Should().Be(DifficultyLevel.Advanced);
        }

        [Fact]
        public void DifficultyLevel_FromString_Should_Throw_InvalidDifficultyLevelException_For_Invalid_Inputs()
        {
            Action act = () => DifficultyLevelExtensions.FromString("Unknown");
            act.Should().Throw<InvalidDifficultyLevelException>();
        }
    }
}
