using System;
using System.Net;
using System.Net.Http;
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

public class UserServiceClientUpdateTests
{
    private static UserServiceOptions ValidOptions() => new() { BaseUrl = "http://userservice:5020" };

    private static UserServiceClient BuildSut(HttpMessageHandler handler, IHttpContextAccessor contextAccessor, UserServiceOptions? options = null)
        => new(new HttpClient(handler), Options.Create(options ?? ValidOptions()), contextAccessor);

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

    private static readonly UpdateUserRequest ValidRequest = new("Updated Name", "operator");

    [Fact]
    public async Task UpdateUserAsync_WhenSuccess_CompletesAndUsesPut()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, NullContextAccessor());

        var id = Guid.NewGuid();
        await sut.UpdateUserAsync(id, ValidRequest);

        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Put);
        handler.CapturedRequest.RequestUri!.ToString().Should().EndWith($"/api/users/{id}");
    }

    [Fact]
    public async Task UpdateUserAsync_ForwardsAuthorizationHeader()
    {
        const string token = "fake-jwt-token";
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, ContextWithToken(token));

        await sut.UpdateUserAsync(Guid.NewGuid(), ValidRequest);

        handler.CapturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.CapturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public async Task UpdateUserAsync_SerializesNameAndRole()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, NullContextAccessor());

        await sut.UpdateUserAsync(Guid.NewGuid(), ValidRequest);

        handler.CapturedBody.Should().Contain("\"name\":\"Updated Name\"");
        handler.CapturedBody.Should().Contain("\"role\":\"operator\"");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateUserAsync_WithEmptyBaseUrl_Throws(string baseUrl)
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, NullContextAccessor(), new UserServiceOptions { BaseUrl = baseUrl });

        var act = async () => await sut.UpdateUserAsync(Guid.NewGuid(), ValidRequest);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BaseUrl*");
    }

    [Fact]
    public async Task UpdateUserAsync_WhenDownstreamFails_ThrowsUserServiceException()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("not found") };
        var sut = BuildSut(new CaptureHandler(response), NullContextAccessor());

        var act = async () => await sut.UpdateUserAsync(Guid.NewGuid(), ValidRequest);

        await act.Should().ThrowAsync<UserServiceException>().WithMessage("*404*");
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? CapturedRequest { get; private set; }
        public string? CapturedBody { get; private set; }

        public CaptureHandler(HttpResponseMessage response) => _response = response;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            if (request.Content is not null)
                CapturedBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return _response;
        }
    }
}
