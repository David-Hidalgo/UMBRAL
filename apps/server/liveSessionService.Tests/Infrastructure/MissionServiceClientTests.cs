using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Infrastructure.Clients;

namespace LiveSessionService.Tests.Infrastructure
{
    public class MissionServiceClientTests
    {
        private static MissionServiceClient BuildSut(HttpMessageHandler handler, string? baseUrl = "http://missionservice:5022")
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["MissionService:BaseUrl"]).Returns(baseUrl);

            return new MissionServiceClient(new HttpClient(handler), configMock.Object);
        }

        [Fact]
        public async Task GetMissionByIdAsync_WithValidResponse_ShouldReturnMissionDto()
        {
            // Arrange
            var missionId = Guid.NewGuid();
            var dto = new MissionClientDto(missionId, "Misión", "Active", 60, Array.Empty<StageClientDto>());
            var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web));
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
            var sut = BuildSut(handler);

            // Act
            var result = await sut.GetMissionByIdAsync(missionId, "Bearer jwt-token");

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(missionId);
            handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest.Headers.Authorization.Parameter.Should().Be("jwt-token");
        }

        [Fact]
        public async Task GetMissionByIdAsync_WhenNotFound_ShouldReturnNull()
        {
            // Arrange
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.NotFound));
            var sut = BuildSut(handler);

            // Act
            var result = await sut.GetMissionByIdAsync(Guid.NewGuid(), "jwt");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetMissionByIdAsync_WhenErrorStatus_ShouldThrowBusinessException()
        {
            // Arrange
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("server error")
            });
            var sut = BuildSut(handler);

            // Act
            Func<Task> act = () => sut.GetMissionByIdAsync(Guid.NewGuid(), "jwt");

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("*Error al consultar el microservicio de misiones*");
        }

        [Fact]
        public async Task GetMissionByIdAsync_WhenInvalidJson_ShouldThrowBusinessException()
        {
            // Arrange
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json")
            });
            var sut = BuildSut(handler);

            // Act
            Func<Task> act = () => sut.GetMissionByIdAsync(Guid.NewGuid(), "jwt");

            // Assert
            await act.Should().ThrowAsync<BusinessException>()
                .WithMessage("*respuesta no válida*");
        }

        [Fact]
        public async Task GetMissionByIdAsync_WhenBaseUrlMissing_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK));
            var sut = BuildSut(handler, baseUrl: "   ");

            // Act
            Func<Task> act = () => sut.GetMissionByIdAsync(Guid.NewGuid(), "jwt");

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("La URL base de MissionService no está configurada.");
        }

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpResponseMessage _response;

            public HttpRequestMessage? LastRequest { get; private set; }

            public StubHandler(HttpResponseMessage response) => _response = response;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(_response);
            }
        }
    }
}
