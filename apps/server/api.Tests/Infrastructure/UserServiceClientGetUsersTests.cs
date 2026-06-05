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

public class UserServiceClientGetUsersTests
{
    // ─── Builders ─────────────────────────────────────────────────────────────

    private static UserServiceOptions ValidOptions() =>
        new() { BaseUrl = "http://userservice:5020" };

    private static UserServiceClient BuildSut(
        HttpMessageHandler handler,
        IHttpContextAccessor? contextAccessor = null,
        UserServiceOptions? options = null)
    {
        var client = new HttpClient(handler);
        var opts = Options.Create(options ?? ValidOptions());
        return new UserServiceClient(client, opts, contextAccessor ?? NullContextAccessor());
    }

    private static IHttpContextAccessor NullContextAccessor()
    {
        var mock = new Mock<IHttpContextAccessor>(MockBehavior.Strict);
        mock.Setup(a => a.HttpContext).Returns((HttpContext?)null);
        return mock.Object;
    }

    private static UserDto SampleUser() =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "user@umbral.io", "User Name", "player", DateTime.UtcNow);

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_WhenServiceReturnsOk_ReturnsUserList()
    {
        // Arrange
        var expected = new List<UserDto> { SampleUser(), SampleUser() };
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        });
        var sut = BuildSut(handler);

        // Act
        var result = await sut.GetUsersAsync();

        // Assert
        result.Should().HaveCount(2);
        handler.CapturedRequest!.Method.Should().Be(HttpMethod.Get);
        handler.CapturedRequest.RequestUri!.ToString().Should().Contain("/api/users");
    }

    [Fact]
    public async Task GetUsersAsync_WhenSearchIsProvided_AppendsSearchQueryParam()
    {
        // Arrange
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<UserDto>())
        });
        var sut = BuildSut(handler);

        // Act
        await sut.GetUsersAsync(search: "john");

        // Assert
        handler.CapturedRequest!.RequestUri!.Query.Should().Contain("search=john");
    }

    [Fact]
    public async Task GetUsersAsync_WhenRoleIsProvided_AppendsRoleQueryParam()
    {
        // Arrange
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<UserDto>())
        });
        var sut = BuildSut(handler);

        // Act
        await sut.GetUsersAsync(role: "admin");

        // Assert
        handler.CapturedRequest!.RequestUri!.Query.Should().Contain("role=admin");
    }

    [Fact]
    public async Task GetUsersAsync_WhenBothFiltersProvided_AppendsBothQueryParams()
    {
        // Arrange
        var handler = new CaptureHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<UserDto>())
        });
        var sut = BuildSut(handler);

        // Act
        await sut.GetUsersAsync(search: "john", role: "player");

        // Assert
        var query = handler.CapturedRequest!.RequestUri!.Query;
        query.Should().Contain("search=john");
        query.Should().Contain("role=player");
    }

    // ─── Error paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUsersAsync_WhenServiceReturnsError_ThrowsUserServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
        {
            Content = new StringContent("upstream error")
        }));

        // Act
        var act = async () => await sut.GetUsersAsync();

        // Assert
        await act.Should().ThrowAsync<UserServiceException>()
            .WithMessage("*503*");
    }

    [Fact]
    public async Task GetUsersAsync_WhenResponseBodyIsEmpty_ReturnsEmptyCollection()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(string.Empty)
        }));

        // Act
        var result = await sut.GetUsersAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUsersAsync_WhenResponseIsNotJson_ThrowsUserServiceException()
    {
        // Arrange
        var sut = BuildSut(new FixedHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not-valid-json")
        }));

        // Act
        var act = async () => await sut.GetUsersAsync();

        // Assert
        await act.Should().ThrowAsync<UserServiceException>()
            .WithMessage("*non-JSON*");
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
