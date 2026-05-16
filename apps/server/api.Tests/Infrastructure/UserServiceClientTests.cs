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
using api.Infrastructure.Clients;
using Xunit;

namespace api.Tests.Infrastructure;

public class UserServiceClientTests
{
    // ─── Builders ─────────────────────────────────────────────────────────────

    private static UserServiceOptions ValidOptions() =>
        new() { BaseUrl = "http://userservice:5020" };

    private static UserServiceClient BuildSut(
        HttpMessageHandler handler,
        IHttpContextAccessor contextAccessor,
        UserServiceOptions? options = null)
    {
        var client = new HttpClient(handler);
        var opts   = Options.Create(options ?? ValidOptions());
        return new UserServiceClient(client, opts, contextAccessor);
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

    private static HttpResponseMessage OkWithId(Guid id) => new(HttpStatusCode.Created)
    {
        Content = JsonContent.Create(new { id })
    };

    private static readonly CreateUserRequest ValidRequest =
        new("admin@umbral.io", "Admin UMBRAL", "Password123!");

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WithValidConfig_ReturnsGuidFromResponse()
    {
        // Arrange
        var expectedId = Guid.NewGuid();
        var handler    = new CaptureHandler(OkWithId(expectedId));
        var sut        = BuildSut(handler, NullContextAccessor());

        // Act
        var result = await sut.CreateUserAsync(ValidRequest);

        // Assert
        result.Should().Be(expectedId);
    }

    [Fact]
    public async Task CreateUserAsync_WithBearerTokenInContext_ForwardsAuthorizationHeader()
    {
        // Arrange
        const string token = "fake-jwt-token";
        var handler = new CaptureHandler(OkWithId(Guid.NewGuid()));
        var sut     = BuildSut(handler, ContextWithToken(token));

        // Act
        await sut.CreateUserAsync(ValidRequest);

        // Assert
        handler.CapturedRequest!.Headers.Authorization.Should().NotBeNull();
        handler.CapturedRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.CapturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task CreateUserAsync_WithNullHttpContext_SendsRequestWithoutAuthHeader()
    {
        // Arrange
        var handler = new CaptureHandler(OkWithId(Guid.NewGuid()));
        var sut     = BuildSut(handler, NullContextAccessor());

        // Act
        await sut.CreateUserAsync(ValidRequest);

        // Assert
        handler.CapturedRequest!.Headers.Authorization.Should().BeNull();
    }

    // ─── Config validation ────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUserAsync_WithEmptyBaseUrl_ThrowsInvalidOperationException(string baseUrl)
    {
        // Arrange
        var handler = new CaptureHandler(OkWithId(Guid.NewGuid()));
        var sut     = BuildSut(handler, NullContextAccessor(), new UserServiceOptions { BaseUrl = baseUrl });

        // Act
        var act = async () => await sut.CreateUserAsync(ValidRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BaseUrl*");
    }

    // ─── HTTP error responses ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WhenUserServiceReturns500_ThrowsInvalidOperationExceptionWithStatusCode()
    {
        // Arrange
        var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Internal error")
        };
        var sut = BuildSut(new CaptureHandler(errorResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.CreateUserAsync(ValidRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*500*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenUserServiceReturns401_ThrowsInvalidOperationExceptionWithStatusCode()
    {
        // Arrange
        var unauthorizedResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("Unauthorized")
        };
        var sut = BuildSut(new CaptureHandler(unauthorizedResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.CreateUserAsync(ValidRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*401*");
    }

    // ─── Body parsing failures ────────────────────────────────────────────────

    [Fact]
    public async Task CreateUserAsync_WhenResponseBodyIsEmpty_ThrowsInvalidOperationException()
    {
        // Arrange
        var emptyBodyResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("")
        };
        var sut = BuildSut(new CaptureHandler(emptyBodyResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.CreateUserAsync(ValidRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenResponseBodyIsNotJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var badJsonResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("not-json-at-all")
        };
        var sut = BuildSut(new CaptureHandler(badJsonResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.CreateUserAsync(ValidRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*non-JSON*");
    }

    [Fact]
    public async Task CreateUserAsync_WhenResponseBodyDeserializesToNull_ThrowsInvalidOperationException()
    {
        // Arrange — "null" es JSON válido pero deserializa a null
        var nullResponse = new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json")
        };
        var sut = BuildSut(new CaptureHandler(nullResponse), NullContextAccessor());

        // Act
        var act = async () => await sut.CreateUserAsync(ValidRequest);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*unexpected response*");
    }

    // ─── Test double ──────────────────────────────────────────────────────────

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
