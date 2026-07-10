using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ScoringService.Application.Commands;
using ScoringService.Application.DTOs;
using ScoringService.Application.Queries;
using ScoringService.Domain.Enums;
using ScoringService.Presentation.Controllers;
using Xunit;

namespace ScoringService.Tests.Presentation
{
    public class ScoringControllerTests
    {
        private readonly Mock<IMediator> _mediator = new();
        private readonly ScoringController _controller;

        private readonly Guid _sessionId = Guid.NewGuid();
        private readonly Guid _teamId = Guid.NewGuid();

        public ScoringControllerTests()
        {
            _controller = new ScoringController(_mediator.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers.Authorization = "Bearer test-token";
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("preferred_username", "operador1")
            }, "test"));
            _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        }

        [Fact]
        public async Task SubmitEvidence_ShouldReturnOkWithResult()
        {
            var expected = new SubmitEvidenceResultDto(Guid.NewGuid(), "Correct", 100, 100);
            _mediator.Setup(m => m.Send(It.IsAny<SubmitEvidenceCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);
            var request = new SubmitEvidenceRequest(SubmissionType.Trivia, Guid.NewGuid(), "answer");

            var result = await _controller.SubmitEvidence(_sessionId, _teamId, request, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
            _mediator.Verify(m => m.Send(It.Is<SubmitEvidenceCommand>(c => c.SessionId == _sessionId && c.TeamId == _teamId), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ValidateEvidence_ShouldReturnOkAndPassActor()
        {
            var submissionId = Guid.NewGuid();
            var expected = new SubmitEvidenceResultDto(submissionId, "Correct", 50, 50);
            _mediator.Setup(m => m.Send(It.IsAny<ValidateEvidenceCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(expected);

            var result = await _controller.ValidateEvidence(_sessionId, submissionId, new ValidateEvidenceRequest(true), CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(expected);
            _mediator.Verify(m => m.Send(It.Is<ValidateEvidenceCommand>(c => c.Actor == "operador1" && c.IsCorrect), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ApplyPenalty_ShouldReturnNoContent()
        {
            _mediator.Setup(m => m.Send(It.IsAny<ApplyPenaltyCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(Unit.Value);

            var result = await _controller.ApplyPenalty(_sessionId, _teamId, new ApplyPenaltyRequest(20, "Retraso"), CancellationToken.None);

            result.Should().BeOfType<NoContentResult>();
            _mediator.Verify(m => m.Send(It.Is<ApplyPenaltyCommand>(c => c.Points == 20 && c.Reason == "Retraso" && c.Actor == "operador1"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetRanking_ShouldReturnOk()
        {
            var ranking = new List<RankingEntryDto> { new(1, _teamId, "Alfa", 90, DateTime.UtcNow) };
            _mediator.Setup(m => m.Send(It.IsAny<GetSessionRankingQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(ranking);

            var result = await _controller.GetRanking(_sessionId, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(ranking);
        }

        [Fact]
        public async Task GetTeamScore_ShouldReturnOk()
        {
            var score = new TeamScoreDto(_sessionId, _teamId, "Alfa", 40);
            _mediator.Setup(m => m.Send(It.IsAny<GetTeamScoreQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(score);

            var result = await _controller.GetTeamScore(_sessionId, _teamId, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(score);
        }

        [Fact]
        public async Task GetEvents_ShouldReturnOk()
        {
            var events = new List<SessionEventDto> { new(Guid.NewGuid(), _sessionId, "SessionCreated", "desc", "op", DateTime.UtcNow) };
            _mediator.Setup(m => m.Send(It.IsAny<GetSessionEventsQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(events);

            var result = await _controller.GetEvents(_sessionId, CancellationToken.None);

            result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(events);
        }
    }
}
