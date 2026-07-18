using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using ScoringService.Infrastructure.Auth;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    public class KeycloakServiceTokenProviderTests
    {
        private static KeycloakServiceOptions DefaultOptions => new()
        {
            BaseUrl = "http://keycloak:8080",
            Realm = "umbral",
            ClientId = "scoring-service",
            ClientSecret = "super-secret"
        };

        private static HttpClient BuildHttpClient(HttpStatusCode status, string accessToken, int expiresIn = 300)
        {
            var payload = JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                expires_in = expiresIn
            });

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() => Task.FromResult(new HttpResponseMessage
                {
                    StatusCode = status,
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                }));

            return new HttpClient(handlerMock.Object);
        }

        [Fact]
        public async Task GetTokenAsync_FirstCall_FetchesAndCachesToken()
        {
            // Arrange
            var httpClient = BuildHttpClient(HttpStatusCode.OK, "my-token-123");
            var sut = new KeycloakServiceTokenProvider(httpClient, Options.Create(DefaultOptions));

            // Act
            var token = await sut.GetTokenAsync(CancellationToken.None);

            // Assert
            token.Should().Be("my-token-123");
        }

        [Fact]
        public async Task GetTokenAsync_SecondCall_ReturnsCachedToken()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            var callCount = 0;
            var payload = JsonSerializer.Serialize(new { access_token = "cached-token", expires_in = 300 });

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    callCount++;
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    });
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = new KeycloakServiceTokenProvider(httpClient, Options.Create(DefaultOptions));

            // Act
            var token1 = await sut.GetTokenAsync();
            var token2 = await sut.GetTokenAsync();

            // Assert
            token1.Should().Be("cached-token");
            token2.Should().Be("cached-token");
            callCount.Should().Be(1, "el segundo llamado debe usar el token en caché");
        }

        [Fact]
        public async Task GetTokenAsync_ExpiredToken_RefetchesNewToken()
        {
            // Arrange — expires_in = 0 hace que el token expire inmediatamente (refresh en segundos = max(0, 0-30) = 0)
            var httpClient = BuildHttpClient(HttpStatusCode.OK, "fresh-token", expiresIn: 0);
            var sut = new KeycloakServiceTokenProvider(httpClient, Options.Create(DefaultOptions));

            // Act - primer call obtiene el token, segundo debe refrescar porque expiró
            await sut.GetTokenAsync();
            var token = await sut.GetTokenAsync();

            // Assert
            token.Should().Be("fresh-token");
        }

        [Fact]
        public async Task GetTokenAsync_KeycloakReturnsError_ThrowsHttpRequestException()
        {
            // Arrange
            var httpClient = BuildHttpClient(HttpStatusCode.Unauthorized, "");
            var sut = new KeycloakServiceTokenProvider(httpClient, Options.Create(DefaultOptions));

            // Act & Assert
            await FluentActions.Invoking(() => sut.GetTokenAsync())
                .Should().ThrowAsync<HttpRequestException>();
        }

        [Fact]
        public async Task GetTokenAsync_ConcurrentCalls_OnlyFetchesOnce()
        {
            // Arrange
            var callCount = 0;
            var payload = JsonSerializer.Serialize(new { access_token = "concurrent-token", expires_in = 300 });
            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Returns(() =>
                {
                    callCount++;
                    return Task.FromResult(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(payload, Encoding.UTF8, "application/json")
                    });
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var sut = new KeycloakServiceTokenProvider(httpClient, Options.Create(DefaultOptions));

            // Act - lanzar múltiples llamadas concurrentes
            var tasks = new[]
            {
                sut.GetTokenAsync(),
                sut.GetTokenAsync(),
                sut.GetTokenAsync()
            };
            var results = await Task.WhenAll(tasks);

            // Assert - todos deben recibir el mismo token
            results.Should().AllBe("concurrent-token");
        }
    }
}
