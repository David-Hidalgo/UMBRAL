using System;
using FluentAssertions;
using MissionService.Domain.ValueObjects;
using Xunit;

namespace MissionService.Tests.Domain
{
    public class MissionStatusTests
    {
        [Fact]
        public void Should_Have_Expected_Values()
        {
            MissionStatus.Inactive.Value.Should().Be("Inactive");
            MissionStatus.Active.Value.Should().Be("Active");
        }

        [Fact]
        public void Inactive_Status_Should_Allow_Modifying_Stages()
        {
            MissionStatus.Inactive.CanModifyStages().Should().BeTrue();
        }

        [Fact]
        public void Active_Status_Should_Block_Modifying_Stages()
        {
            MissionStatus.Active.CanModifyStages().Should().BeFalse();
        }

        [Theory]
        [InlineData("Inactive")]
        [InlineData("inactive")]
        [InlineData(" INACTIVE ")]
        public void FromString_Should_Return_Inactive_For_Valid_Inputs(string input)
        {
            var status = MissionStatus.FromString(input);
            status.Should().Be(MissionStatus.Inactive);
        }

        [Theory]
        [InlineData("Active")]
        [InlineData("active")]
        [InlineData(" ACTIVE ")]
        public void FromString_Should_Return_Active_For_Valid_Inputs(string input)
        {
            var status = MissionStatus.FromString(input);
            status.Should().Be(MissionStatus.Active);
        }

        [Fact]
        public void FromString_Should_Throw_ArgumentException_For_Invalid_Inputs()
        {
            Action act = () => MissionStatus.FromString("Unknown");
            act.Should().Throw<ArgumentException>();
        }
    }
}
