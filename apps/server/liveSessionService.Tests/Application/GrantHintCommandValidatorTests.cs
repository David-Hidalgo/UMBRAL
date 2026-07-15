using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;

namespace LiveSessionService.Tests.Application
{
    public class GrantHintCommandValidatorTests
    {
        private readonly GrantHintCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldBeValid()
        {
            var command = new GrantHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "jwt");
            _validator.Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithEmptySessionId_ShouldBeInvalid()
        {
            var command = new GrantHintCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "jwt");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithEmptyTeamId_ShouldBeInvalid()
        {
            var command = new GrantHintCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "jwt");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithEmptySubStageId_ShouldBeInvalid()
        {
            var command = new GrantHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "jwt");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithEmptyClueId_ShouldBeInvalid()
        {
            var command = new GrantHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "jwt");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }
    }
}
