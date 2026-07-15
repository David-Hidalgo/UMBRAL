using System;
using FluentAssertions;
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Validators;

namespace LiveSessionService.Tests.Application
{
    public class RequestHintCommandValidatorTests
    {
        private readonly RequestHintCommandValidator _validator = new();

        [Fact]
        public void Validate_WithValidCommand_ShouldBeValid()
        {
            var command = new RequestHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "user-1");
            _validator.Validate(command).IsValid.Should().BeTrue();
        }

        [Fact]
        public void Validate_WithEmptySessionId_ShouldBeInvalid()
        {
            var command = new RequestHintCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "user-1");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithEmptyTeamId_ShouldBeInvalid()
        {
            var command = new RequestHintCommand(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "user-1");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithEmptySubStageId_ShouldBeInvalid()
        {
            var command = new RequestHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "user-1");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }

        [Fact]
        public void Validate_WithEmptyKeycloakUserId_ShouldBeInvalid()
        {
            var command = new RequestHintCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "");
            _validator.Validate(command).IsValid.Should().BeFalse();
        }
    }
}
