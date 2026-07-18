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
        private readonly Mock<IMediator> _mediatorMock;
        private readonly ScoringController _controller;

        public ScoringControllerTests()
        {
            _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
            _controller = new ScoringController(_mediatorMock.Object);
        }

        private void SetupUserClaim(string claimType, string claimValue)
        {
            var claims = new List<Claim> { new Claim(claimType, claimValue) };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task SubmitEvidence_ReturnsOkWithResult()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var request = new SubmitEvidenceRequest(SubmissionType.Trivia, Guid.NewGuid(), "Correct Answer");

            var expectedResult = new SubmitEvidenceResultDto(Guid.NewGuid(), "Approved", 10, 100);

            _mediatorMock.Setup(m => m.Send(It.Is<SubmitEvidenceCommand>(c =>
                c.SessionId == sessionId &&
                c.TeamId == teamId &&
                c.SubStageId == request.SubStageId &&
                c.Type == request.Type &&
                c.Payload == request.Payload), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SubmitEvidence(sessionId, teamId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ValidateEvidence_ReturnsOkWithResult()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var submissionId = Guid.NewGuid();
            var request = new ValidateEvidenceRequest(true);
            var expectedResult = new SubmitEvidenceResultDto(submissionId, "Approved", 15, 115);

            SetupUserClaim("preferred_username", "admin-user");

            _mediatorMock.Setup(m => m.Send(It.Is<ValidateEvidenceCommand>(c =>
                c.SessionId == sessionId &&
                c.SubmissionId == submissionId &&
                c.IsCorrect == request.IsCorrect &&
                c.Actor == "admin-user"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.ValidateEvidence(sessionId, submissionId, request, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedResult);
        }

        [Fact]
        public async Task ApplyPenalty_ReturnsNoContent()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var request = new ApplyPenaltyRequest(10, "HintUsed");

            SetupUserClaim("name", "operator-user");

            _mediatorMock.Setup(m => m.Send(It.Is<ApplyPenaltyCommand>(c =>
                c.SessionId == sessionId &&
                c.TeamId == teamId &&
                c.Points == request.Points &&
                c.Reason == request.Reason &&
                c.Actor == "operator-user"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.ApplyPenalty(sessionId, teamId, request, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task GetRanking_ReturnsOkWithRanking()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var expectedRanking = new List<RankingEntryDto>
            {
                new RankingEntryDto(1, Guid.NewGuid(), "Team A", 100, DateTime.UtcNow)
            };

            _mediatorMock.Setup(m => m.Send(It.Is<GetSessionRankingQuery>(q => q.SessionId == sessionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedRanking);

            // Act
            var result = await _controller.GetRanking(sessionId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedRanking);
        }

        [Fact]
        public async Task GetCurrentChallenge_ReturnsOkWithChallenge()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var question = new PlayerQuestionDto(Guid.NewGuid(), "Q?", new List<PlayerAnswerDto>());
            var subStage = new PlayerSubStageDto(Guid.NewGuid(), "Trivia", 10, SubmissionType.Trivia, question, "Objective", false, null, null, null);
            var expectedChallenge = new PlayerChallengeDto(sessionId, "Active", new List<PlayerSubStageDto> { subStage });

            _mediatorMock.Setup(m => m.Send(It.Is<GetCurrentChallengeQuery>(q =>
                q.SessionId == sessionId && q.TeamId == teamId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedChallenge);

            // Act
            var result = await _controller.GetCurrentChallenge(sessionId, teamId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedChallenge);
        }

        [Fact]
        public async Task GetTeamScore_ReturnsOkWithScore()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var expectedScore = new TeamScoreDto(sessionId, teamId, "Alfa", 150);

            _mediatorMock.Setup(m => m.Send(It.Is<GetTeamScoreQuery>(q =>
                q.SessionId == sessionId && q.TeamId == teamId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedScore);

            // Act
            var result = await _controller.GetTeamScore(sessionId, teamId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedScore);
        }

        [Fact]
        public async Task GetStageProgress_ReturnsOkWithProgress()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var expectedProgress = new StageProgressDto(1, new List<TeamStageProgressDto>(), false);

            _mediatorMock.Setup(m => m.Send(It.Is<GetStageProgressQuery>(q =>
                q.SessionId == sessionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedProgress);

            // Act
            var result = await _controller.GetStageProgress(sessionId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedProgress);
        }

        [Fact]
        public async Task GetEvents_ReturnsOkWithEvents()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var expectedEvents = new List<SessionEventDto>
            {
                new SessionEventDto(Guid.NewGuid(), sessionId, "ClueRequested", "Detail", "actor", DateTime.UtcNow)
            };

            _mediatorMock.Setup(m => m.Send(It.Is<GetSessionEventsQuery>(q =>
                q.SessionId == sessionId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedEvents);

            // Act
            var result = await _controller.GetEvents(sessionId, CancellationToken.None);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(expectedEvents);
        }
    }
}
