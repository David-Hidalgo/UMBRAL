using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using api.Application.DTOs;
using api.Application.Exceptions;
using api.Infrastructure.Clients;
using Xunit;

namespace api.Tests.Infrastructure;

public class MissionServiceClientWriteTests
{
    // ─── Builders ─────────────────────────────────────────────────────────────

    private static MissionServiceOptions ValidOptions() =>
        new() { BaseUrl = "http://missionservice:5030" };

    private static MissionServiceClient BuildSut(
        HttpMessageHandler handler,
        IHttpContextAccessor? contextAccessor = null,
        MissionServiceOptions? options = null)
    {
        var client = new HttpClient(handler);
        var opts = Options.Create(options ?? ValidOptions());
        return new MissionServiceClient(client, opts, contextAccessor ?? NullContextAccessor());
    }

    private static IHttpContextAccessor NullContextAccessor()
    {
        var mock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        mock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        return mock.Object;
    }

    private static HttpResponseMessage NoContent() =>
        new(HttpStatusCode.NoContent) { Content = new StringContent(string.Empty) };

    private static HttpResponseMessage ErrorResponse(HttpStatusCode status = HttpStatusCode.InternalServerError) =>
        new(status) { Content = new StringContent("upstream error") };

    private static readonly CreateMissionRequest ValidCreateRequest =
        new("Mission Alpha", "Desc", "Beginner", 60, new List<CreateStageRequest>());

    private static readonly UpdateMissionRequest ValidUpdateRequest =
        new("Updated Name", null, null, null, null);

    private static readonly CreateStageRequest ValidStageRequest =
        new("Stage 1", "Desc", 1, new List<CreateSubStageRequest>());

    // ─── CreateMissionAsync — error paths ─────────────────────────────────────

    [Fact]
    public async Task CreateMissionAsync_WhenBaseUrlIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(NoContent()), options: new MissionServiceOptions { BaseUrl = "" });

        // Act
        var act = async () => await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

    [Fact]
    public async Task CreateMissionAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.ServiceUnavailable)));

        // Act
        var act = async () => await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*503*");
    }

    [Fact]
    public async Task CreateMissionAsync_WhenResponseBodyIsEmpty_ThrowsMissionServiceException()
    {
        // Arrange
        var emptyBody = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(string.Empty)
        };
        var sut = BuildSut(new FixedHandler(emptyBody));

        // Act
        var act = async () => await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*empty response*");
    }

    [Fact]
    public async Task CreateMissionAsync_WhenResponseIsNotJson_ThrowsMissionServiceException()
    {
        // Arrange
        var badJson = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("not-json")
        };
        var sut = BuildSut(new FixedHandler(badJson));

        // Act
        var act = async () => await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*non-JSON*");
    }

    [Fact]
    public async Task CreateMissionAsync_WhenResponsePayloadIsNull_ThrowsMissionServiceException()
    {
        // Arrange — JSON "null" deserializes to null record
        var nullPayload = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
        var sut = BuildSut(new FixedHandler(nullPayload));

        // Act
        var act = async () => await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*unexpected response*");
    }

    // ─── UpdateMissionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateMissionAsync_WhenServiceReturns204_CompletesSuccessfully()
    {
        // Arrange
        var handler = new CaptureHandler(NoContent());
        var sut = BuildSut(handler);
        var missionId = Guid.NewGuid();

        // Act
        var act = async () => await sut.UpdateMissionAsync(missionId, ValidUpdateRequest);

        // Assert
        await act.Should().NotThrowAsync();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Patch);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain(missionId.ToString());
    }

    [Fact]
    public async Task UpdateMissionAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.NotFound)));

        // Act
        var act = async () => await sut.UpdateMissionAsync(Guid.NewGuid(), ValidUpdateRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*404*");
    }

    // ─── DeleteMissionAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteMissionAsync_WhenServiceReturns204_CompletesSuccessfully()
    {
        // Arrange
        var handler = new CaptureHandler(NoContent());
        var sut = BuildSut(handler);
        var missionId = Guid.NewGuid();

        // Act
        var act = async () => await sut.DeleteMissionAsync(missionId);

        // Assert
        await act.Should().NotThrowAsync();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain(missionId.ToString());
    }

    [Fact]
    public async Task DeleteMissionAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.NotFound)));

        // Act
        var act = async () => await sut.DeleteMissionAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*404*");
    }

    // ─── AddStageAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AddStageAsync_WhenServiceReturns201_ReturnsStageId()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new { id = expectedId })
        });
        var sut = BuildSut(handler);
        var missionId = Guid.NewGuid();

        // Act
        var result = await sut.AddStageAsync(missionId, ValidStageRequest);

        // Assert
        result.Should().Be(expectedId);
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Post);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain($"{missionId}/stages");
    }

    [Fact]
    public async Task AddStageAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.BadRequest)));

        // Act
        var act = async () => await sut.AddStageAsync(Guid.NewGuid(), ValidStageRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*400*");
    }

    [Fact]
    public async Task AddStageAsync_WhenResponseBodyIsEmpty_ThrowsMissionServiceException()
    {
        // Arrange
        var emptyBody = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent(string.Empty)
        };
        var sut = BuildSut(new FixedHandler(emptyBody));

        // Act
        var act = async () => await sut.AddStageAsync(Guid.NewGuid(), ValidStageRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*empty response*");
    }

    [Fact]
    public async Task AddStageAsync_WhenResponseIsNotJson_ThrowsMissionServiceException()
    {
        // Arrange
        var badJson = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("not-json")
        };
        var sut = BuildSut(new FixedHandler(badJson));

        // Act
        var act = async () => await sut.AddStageAsync(Guid.NewGuid(), ValidStageRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*non-JSON*");
    }

    // ─── UpdateStageAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStageAsync_WhenServiceReturns204_CompletesSuccessfully()
    {
        // Arrange
        var handler = new CaptureHandler(NoContent());
        var sut = BuildSut(handler);
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        // Act
        var act = async () => await sut.UpdateStageAsync(missionId, stageId, ValidStageRequest);

        // Assert
        await act.Should().NotThrowAsync();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Put);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain($"{missionId}/stages/{stageId}");
    }

    [Fact]
    public async Task UpdateStageAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.NotFound)));

        // Act
        var act = async () => await sut.UpdateStageAsync(Guid.NewGuid(), Guid.NewGuid(), ValidStageRequest);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*404*");
    }

    // ─── DeleteStageAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteStageAsync_WhenServiceReturns204_CompletesSuccessfully()
    {
        // Arrange
        var handler = new CaptureHandler(NoContent());
        var sut = BuildSut(handler);
        var missionId = Guid.NewGuid();
        var stageId = Guid.NewGuid();

        // Act
        var act = async () => await sut.DeleteStageAsync(missionId, stageId);

        // Assert
        await act.Should().NotThrowAsync();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain($"{missionId}/stages/{stageId}");
    }

    [Fact]
    public async Task DeleteStageAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.NotFound)));

        // Act
        var act = async () => await sut.DeleteStageAsync(Guid.NewGuid(), Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*404*");
    }

    // ─── ReorderStagesAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderStagesAsync_WhenServiceReturns204_CompletesSuccessfully()
    {
        // Arrange
        var handler = new CaptureHandler(NoContent());
        var sut = BuildSut(handler);
        var missionId = Guid.NewGuid();
        var stageIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var act = async () => await sut.ReorderStagesAsync(missionId, stageIds);

        // Assert
        await act.Should().NotThrowAsync();
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Put);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain($"{missionId}/stages/reorder");
    }

    [Fact]
    public async Task ReorderStagesAsync_WhenServiceReturnsError_ThrowsMissionServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(ErrorResponse(HttpStatusCode.UnprocessableEntity)));

        // Act
        var act = async () => await sut.ReorderStagesAsync(Guid.NewGuid(), new List<Guid>());

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*422*");
    }

    // ─── Test doubles ──────────────────────────────────────────────────────────

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? CapturedRequest { get; private set; }

        public CaptureHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(_response);
        }
    }

    private sealed class FixedHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FixedHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }
}
