using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
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

public class MissionServiceClientTests
{
    // ─── Builders ─────────────────────────────────────────────────────────────

    private static MissionServiceOptions ValidOptions() =>
        new() { BaseUrl = "http://missionservice:5030" };

    private static MissionServiceClient BuildSut(
        HttpMessageHandler handler,
        IHttpContextAccessor contextAccessor,
        MissionServiceOptions? options = null)
    {
        var client = new HttpClient(handler);
        var opts   = Options.Create(options ?? ValidOptions());
        return new MissionServiceClient(client, opts, contextAccessor);
    }

    private static IHttpContextAccessor NullContextAccessor()
    {
        var mock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        mock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        return mock.Object;
    }

    private static IHttpContextAccessor ContextWithToken(string bearerToken)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = $"Bearer {bearerToken}";
        var mock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        mock.Setup(a => a.HttpContext).Returns(context);
        return mock.Object;
    }

    private static HttpResponseMessage OkWithMissions(IEnumerable<MissionSummaryDto> missions) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(missions)
    };

    private static HttpResponseMessage CreatedWithId(Guid id) => new(HttpStatusCode.Created)
    {
        Content = JsonContent.Create(new { id })
    };

    private static readonly CreateMissionRequest ValidCreateRequest =
        new("Mission Alpha", "Description Alpha", new List<CreateStageRequest>());

    // ─── Tests for GetMissionsAsync ───────────────────────────────────────────

    [Fact]
    public async Task GetMissionsAsync_WithValidConfig_ReturnsMissionsFromResponse()
    {
        // Arrange
        var expectedMissions = new List<MissionSummaryDto>
        {
            new(Guid.NewGuid(), "Mission 1", "Desc 1", "Active", 3),
            new(Guid.NewGuid(), "Mission 2", "Desc 2", "Inactive", 1)
        };
        var handler = new CaptureHandler(OkWithMissions(expectedMissions));
        var sut     = BuildSut(handler, NullContextAccessor());

        // Act
        var result = await sut.GetMissionsAsync(null, null);

        // Assert
        result.Should().BeEquivalentTo(expectedMissions);
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Get);
        handler.CapturedRequest.RequestUri!.ToString().Should().Be("http://missionservice:5030/api/missions");
    }

    [Fact]
    public async Task GetMissionsAsync_WhenServiceReturns502_ThrowsMissionServiceException()
    {
        // Arrange
        var errorResponse = new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("Bad Gateway error from upstream service")
        };
        var sut = BuildSut(new CaptureHandler(errorResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.GetMissionsAsync(null, null);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*502*");
    }

    [Fact]
    public async Task GetMissionsAsync_ConstructsQueryStringCorrectly_WithBothFilters()
    {
        // Arrange
        var handler = new CaptureHandler(OkWithMissions([]));
        var sut     = BuildSut(handler, NullContextAccessor());

        // Act
        await sut.GetMissionsAsync("search term", "Active");

        // Assert
        handler.CapturedRequest!.RequestUri!.Query.Should().Contain("search=search+term");
        handler.CapturedRequest.RequestUri.Query.Should().Contain("status=Active");
    }

    [Fact]
    public async Task GetMissionsAsync_WithBearerTokenInContext_ForwardsAuthorizationHeader()
    {
        // Arrange
        const string token = "custom-jwt-token-missions";
        var handler = new CaptureHandler(OkWithMissions([]));
        var sut     = BuildSut(handler, ContextWithToken(token));

        // Act
        await sut.GetMissionsAsync(null, null);

        // Assert
        handler.CapturedRequest!.Headers.Authorization.Should().NotBeNull();
        handler.CapturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.CapturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task GetMissionsAsync_WhenResponseBodyIsNotJson_ThrowsMissionServiceException()
    {
        // Arrange
        var badJsonResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json-string")
        };
        var sut = BuildSut(new CaptureHandler(badJsonResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.GetMissionsAsync(null, null);

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*non-JSON*");
    }

    [Fact]
    public async Task GetMissionsAsync_WithEmptyBaseUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new CaptureHandler(OkWithMissions([]));
        var sut     = BuildSut(handler, NullContextAccessor(), new MissionServiceOptions { BaseUrl = "" });

        // Act
        var act = async () => await sut.GetMissionsAsync(null, null);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

    // ─── Tests for CreateMissionAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateMissionAsync_WithValidRequest_ReturnsGuidFromResponse()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler    = new CaptureHandler(CreatedWithId(expectedId));
        var sut        = BuildSut(handler, NullContextAccessor());

        // Act
        var result = await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        result.Should().Be(expectedId);
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Post);
        handler.CapturedRequest.RequestUri!.ToString().Should().Be("http://missionservice:5030/api/missions");
    }

    [Fact]
    public async Task CreateMissionAsync_WithBearerTokenInContext_ForwardsAuthorizationHeader()
    {
        // Arrange
        const string token = "fake-jwt-token";
        var handler = new CaptureHandler(CreatedWithId(Guid.NewGuid()));
        var sut     = BuildSut(handler, ContextWithToken(token));

        // Act
        await sut.CreateMissionAsync(ValidCreateRequest);

        // Assert
        handler.CapturedRequest!.Headers.Authorization.Should().NotBeNull();
        handler.CapturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.CapturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    // ─── Test double ──────────────────────────────────────────────────────────

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedBody { get; private set; }

        public CaptureHandler(HttpResponseMessage response) => _response = response;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content is not null)
            {
                CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
            }
            return _response;
        }
    }
}
