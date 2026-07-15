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
using Xunit;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Presentation.Controllers;
using Unit = MediatR.Unit;

namespace LiveSessionService.Tests.Presentation
{
    public class LiveSessionsControllerTests
    {
        private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
        private readonly LiveSessionsController _controller;

        public LiveSessionsControllerTests()
        {
            _controller = new LiveSessionsController(_mediatorMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        [Fact]
        public async Task Create_WithValidRequest_ShouldReturnCreatedAtAction()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var missionId = Guid.NewGuid();
            _controller.HttpContext.Request.Headers.Authorization = "Bearer jwt-token";

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateLiveSessionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sessionId);

            // Act
            var result = await _controller.Create(
                new CreateLiveSessionRequest(missionId, "Partida 1", null),
                CancellationToken.None);

            // Assert
            result.Should().BeOfType<CreatedAtActionResult>();
            var created = (CreatedAtActionResult)result;
            created.ActionName.Should().Be(nameof(LiveSessionsController.GetById));
            created.Value.Should().Be(sessionId);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOkWithSessions()
        {
            // Arrange
            var summaries = new List<LiveSessionSummaryDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Misión", "Partida", null, "Programada", 1, 2)
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetLiveSessionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(summaries);

            // Act
            var result = await _controller.GetAll(null, null, CancellationToken.None);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(summaries);
        }

        [Fact]
        public async Task GetById_WhenSessionExists_ShouldReturnOk()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var detail = new LiveSessionDetailDto(
                sessionId,
                Guid.NewGuid(),
                "Misión",
                "Partida",
                null,
                0,
                "Programada",
                new List<TeamDto>(),
                0,
                null,
                null);

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetLiveSessionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(detail);

            // Act
            var result = await _controller.GetById(sessionId, CancellationToken.None);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().Be(detail);
        }

        [Fact]
        public async Task GetById_WhenSessionNotFound_ShouldReturnNotFound()
        {
            // Arrange
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetLiveSessionByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((LiveSessionDetailDto?)null);

            // Act
            var result = await _controller.GetById(Guid.NewGuid(), CancellationToken.None);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task TransitionStatus_ShouldReturnNoContent()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _controller.HttpContext.Request.Headers.Authorization = "Bearer jwt-token";

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<TransitionSessionCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.TransitionStatus(
                sessionId,
                new TransitionStatusRequest(LiveSessionStatus.Activa),
                CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task CreateTeam_ShouldReturnCreatedWithTeamId()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<CreateTeamCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(teamId);

            // Act
            var result = await _controller.CreateTeam(
                sessionId,
                new CreateTeamRequest("Alfa"),
                CancellationToken.None);

            // Assert
            var created = result.Should().BeOfType<CreatedResult>().Subject;
            created.Value.Should().Be(teamId);
        }

        [Fact]
        public async Task JoinTeam_WithValidClaims_ShouldReturnNoContent()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            SetUserClaims("user-123", "Juan");

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<JoinTeamCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.JoinTeam(sessionId, teamId, CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task JoinTeam_WithoutUserClaims_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.JoinTeam(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("No se pudo identificar al usuario desde el token JWT.");
        }

        [Fact]
        public async Task AdvanceStage_ShouldReturnNoContent()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            _controller.HttpContext.Request.Headers.Authorization = "Bearer jwt-token";

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<AdvanceStageCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.AdvanceStage(
                sessionId,
                new AdvanceStageRequest(2),
                CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task RequestHint_WithValidClaims_ShouldReturnCreatedWithRequestId()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var requestId = Guid.NewGuid();
            SetUserClaims("user-123", "Juan");

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<RequestHintCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requestId);

            // Act
            var result = await _controller.RequestHint(
                sessionId, teamId, new RequestHintRequest(Guid.NewGuid()), CancellationToken.None);

            // Assert
            var created = result.Should().BeOfType<CreatedResult>().Subject;
            created.Value.Should().Be(requestId);
        }

        [Fact]
        public async Task RequestHint_WithoutUserClaims_ShouldReturnBadRequest()
        {
            // Act
            var result = await _controller.RequestHint(
                Guid.NewGuid(), Guid.NewGuid(), new RequestHintRequest(Guid.NewGuid()), CancellationToken.None);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("No se pudo identificar al usuario desde el token JWT.");
        }

        [Fact]
        public async Task GetHintRequests_ShouldReturnOkWithRequests()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var requests = new List<HintRequestDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), "Alfa", Guid.NewGuid(), "Pendiente", DateTime.UtcNow, null)
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetHintRequestsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(requests);

            // Act
            var result = await _controller.GetHintRequests(sessionId, "Pendiente", CancellationToken.None);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(requests);
        }

        [Fact]
        public async Task GrantHint_ShouldReturnNoContent()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            _controller.HttpContext.Request.Headers.Authorization = "Bearer jwt-token";

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GrantHintCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Unit.Value);

            // Act
            var result = await _controller.GrantHint(
                sessionId, teamId, new GrantHintRequest(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task GetTeamHints_ShouldReturnOkWithReleasedHints()
        {
            // Arrange
            var sessionId = Guid.NewGuid();
            var teamId = Guid.NewGuid();
            var hints = new List<ReleasedHintDto>
            {
                new(Guid.NewGuid(), teamId, Guid.NewGuid(), Guid.NewGuid(), "Pista", 1, DateTime.UtcNow)
            };

            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetTeamReleasedHintsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(hints);

            // Act
            var result = await _controller.GetTeamHints(sessionId, teamId, CancellationToken.None);

            // Assert
            var ok = result.Should().BeOfType<OkObjectResult>().Subject;
            ok.Value.Should().BeEquivalentTo(hints);
        }

        private void SetUserClaims(string userId, string displayName)
        {
            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("name", displayName)
            }, "TestAuth");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }
    }
}
