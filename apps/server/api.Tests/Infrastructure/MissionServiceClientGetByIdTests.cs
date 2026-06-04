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

public class MissionServiceClientGetByIdTests
{
    private static MissionServiceOptions ValidOptions() =>
        new() { BaseUrl = "http://missionservice:5030" };

    private static MissionServiceClient BuildSut(
        HttpMessageHandler handler,
        IHttpContextAccessor contextAccessor,
        MissionServiceOptions? options = null)
    {
        var client = new HttpClient(handler);
        var opts = Options.Create(options ?? ValidOptions());
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

    private static MissionDetailDto SampleMissionDetail(Guid missionId) =>
        new(
            missionId,
            "Mission Alpha",
            "Full mission description",
            "Active",
            new List<StageDetailDto>
            {
                new(
                    Guid.NewGuid(),
                    "Stage 1",
                    "Stage description",
                    0,
                    new List<SubStageDetailDto>
                    {
                        new(
                            Guid.NewGuid(),
                            "Trivia 1",
                            0,
                            10,
                            new QuestionDetailDto(
                                Guid.NewGuid(),
                                "What is 2+2?",
                                new List<AnswerDetailDto>
                                {
                                    new(Guid.NewGuid(), "4", true),
                                    new(Guid.NewGuid(), "5", false)
                                }),
                            null,
                            null,
                            null)
                    })
            });

    private static HttpResponseMessage OkWithMissionDetail(MissionDetailDto detail) => new(HttpStatusCode.OK)
    {
        Content = JsonContent.Create(detail)
    };

    [Fact]
    public async Task GetMissionByIdAsync_WithValidId_ReturnsMissionDetailFromResponse()
    {
        // Arrange
        var missionId = Guid.NewGuid();
        var expected = SampleMissionDetail(missionId);
        var handler = new CaptureHandler(OkWithMissionDetail(expected));
        var sut = BuildSut(handler, NullContextAccessor());

        // Act
        var result = await sut.GetMissionByIdAsync(missionId);

        // Assert
        result.Should().BeEquivalentTo(expected);
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Get);
        handler.CapturedRequest.RequestUri!.ToString().Should().Be($"http://missionservice:5030/api/missions/{missionId}");
    }

    [Fact]
    public async Task GetMissionByIdAsync_WhenServiceReturns404_ThrowsMissionServiceExceptionWith404()
    {
        // Arrange
        var notFoundResponse = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"error\":\"Mission with ID '...' was not found.\"}")
        };
        var sut = BuildSut(new CaptureHandler(notFoundResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.GetMissionByIdAsync(Guid.NewGuid());

        // Assert
        var ex = await act.Should().ThrowAsync<MissionServiceException>();
        ex.Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task GetMissionByIdAsync_WithBearerTokenInContext_ForwardsAuthorizationHeader()
    {
        // Arrange
        const string token = "custom-jwt-token-missions";
        var missionId = Guid.NewGuid();
        var handler = new CaptureHandler(OkWithMissionDetail(SampleMissionDetail(missionId)));
        var sut = BuildSut(handler, ContextWithToken(token));

        // Act
        await sut.GetMissionByIdAsync(missionId);

        // Assert
        handler.CapturedRequest!.Headers.Authorization.Should().NotBeNull();
        handler.CapturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.CapturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task GetMissionByIdAsync_WhenResponseBodyIsNotJson_ThrowsMissionServiceException()
    {
        // Arrange
        var badJsonResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json-string")
        };
        var sut = BuildSut(new CaptureHandler(badJsonResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.GetMissionByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<MissionServiceException>()
            .WithMessage("*non-JSON*");
    }

    [Fact]
    public async Task GetMissionByIdAsync_WithEmptyBaseUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = new CaptureHandler(OkWithMissionDetail(SampleMissionDetail(Guid.NewGuid())));
        var sut = BuildSut(handler, NullContextAccessor(), new MissionServiceOptions { BaseUrl = "" });

        // Act
        var act = async () => await sut.GetMissionByIdAsync(Guid.NewGuid());

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

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
}
