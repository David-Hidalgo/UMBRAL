using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using api.Application.Exceptions;
using api.Infrastructure.Clients;
using Xunit;

namespace api.Tests.Infrastructure;

public class UserServiceClientDeleteTests
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

    [Fact]
    public async Task DeleteUserAsync_WhenSuccess_CompletesAndUsesDelete()
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, NullContextAccessor());

        await sut.DeleteUserAsync("keycloak-id-123");

        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Delete);
        handler.CapturedRequest.RequestUri!.ToString().Should().EndWith("/api/users/keycloak-id-123");
    }

    [Fact]
    public async Task DeleteUserAsync_ForwardsAuthorizationHeader()
    {
        const string token = "fake-jwt-token";
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, ContextWithToken(token));

        await sut.DeleteUserAsync("some-id");

        handler.CapturedRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.CapturedRequest.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteUserAsync_WithEmptyBaseUrl_ThrowsInvalidOperationException(string baseUrl)
    {
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        var sut = BuildSut(handler, NullContextAccessor(), new UserServiceOptions { BaseUrl = baseUrl });

        var act = async () => await sut.DeleteUserAsync("any-id");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*BaseUrl*");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenNotFound_ThrowsUserServiceException404()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("not found") };
        var sut = BuildSut(new CaptureHandler(response), NullContextAccessor());

        var act = async () => await sut.DeleteUserAsync("missing-id");

        var ex = await act.Should().ThrowAsync<UserServiceException>();
        ex.Which.StatusCode.Should().Be(404);
        ex.Which.Message.Should().Contain("404");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenConflict_ThrowsUserServiceException409()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("already inactive") };
        var sut = BuildSut(new CaptureHandler(response), NullContextAccessor());

        var act = async () => await sut.DeleteUserAsync("inactive-id");

        var ex = await act.Should().ThrowAsync<UserServiceException>();
        ex.Which.StatusCode.Should().Be(409);
        ex.Which.Message.Should().Contain("409");
    }

    [Fact]
    public async Task DeleteUserAsync_WhenOtherError_ThrowsUserServiceException502()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("server error") };
        var sut = BuildSut(new CaptureHandler(response), NullContextAccessor());

        var act = async () => await sut.DeleteUserAsync("any-id");

        var ex = await act.Should().ThrowAsync<UserServiceException>();
        ex.Which.StatusCode.Should().Be(502);
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public HttpRequestMessage? CapturedRequest { get; private set; }

        public CaptureHandler(HttpResponseMessage response) => _response = response;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            return Task.FromResult(_response);
        }
    }
}
