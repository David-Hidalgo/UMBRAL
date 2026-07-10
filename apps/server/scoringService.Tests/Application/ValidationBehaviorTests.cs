using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using MediatR;
using ScoringService.Application.Commands;
using ScoringService.Application.Common;
using ScoringService.Application.Validators;
using ScoringService.Domain.Enums;
using Xunit;

namespace ScoringService.Tests.Application
{
    public class ValidationBehaviorTests
    {
        private static ApplyPenaltyCommand ValidCommand()
            => new(Guid.NewGuid(), Guid.NewGuid(), 10, "Motivo", "op");

        [Fact]
        public async Task Handle_WithValidRequest_ShouldCallNext()
        {
            var behavior = new ValidationBehavior<ApplyPenaltyCommand, Unit>(new[] { new ApplyPenaltyCommandValidator() });
            var nextCalled = false;
            RequestHandlerDelegate<Unit> next = (_) => { nextCalled = true; return Task.FromResult(Unit.Value); };

            await behavior.Handle(ValidCommand(), next, CancellationToken.None);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
        {
            var behavior = new ValidationBehavior<ApplyPenaltyCommand, Unit>(new[] { new ApplyPenaltyCommandValidator() });
            var invalid = new ApplyPenaltyCommand(Guid.Empty, Guid.Empty, 0, "", "op");
            RequestHandlerDelegate<Unit> next = (_) => Task.FromResult(Unit.Value);

            Func<Task> act = () => behavior.Handle(invalid, next, CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
        }

        [Fact]
        public async Task Handle_WithNoValidators_ShouldCallNext()
        {
            var behavior = new ValidationBehavior<ApplyPenaltyCommand, Unit>(Array.Empty<IValidator<ApplyPenaltyCommand>>());
            var nextCalled = false;
            RequestHandlerDelegate<Unit> next = (_) => { nextCalled = true; return Task.FromResult(Unit.Value); };

            await behavior.Handle(ValidCommand(), next, CancellationToken.None);

            nextCalled.Should().BeTrue();
        }
    }
}
