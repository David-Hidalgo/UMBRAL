using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using ScoringService.Application.Abstractions;
using ScoringService.Domain.Enums;
using ScoringService.Infrastructure.Clients;
using Xunit;

namespace ScoringService.Tests.Infrastructure
{
    public class MissionContentClientTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode _status;
            private readonly string _body;
            public HttpRequestMessage? LastRequest { get; private set; }

            public StubHandler(HttpStatusCode status, string body)
            {
                _status = status;
                _body = body;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(new HttpResponseMessage(_status)
                {
                    Content = new StringContent(_body, Encoding.UTF8, "application/json")
                });
            }
        }

        private static MissionContentClient BuildClient(StubHandler handler)
        {
            var httpClient = new HttpClient(handler);
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["MissionService:BaseUrl"] = "http://mission:5022" })
                .Build();
            var tokenProvider = new Mock<IServiceTokenProvider>();
            tokenProvider
                .Setup(t => t.GetTokenAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync("service-token");
            return new MissionContentClient(httpClient, tokenProvider.Object, config);
        }

        [Fact]
        public async Task GetSubStageContentAsync_ForTrivia_ShouldReturnCorrectAnswers()
        {
            var missionId = Guid.NewGuid();
            var subId = Guid.NewGuid();
            var correct = Guid.NewGuid();
            var wrong = Guid.NewGuid();
            var json = $$"""
            {
              "id": "{{missionId}}",
              "stages": [
                { "id": "{{Guid.NewGuid()}}", "subStages": [
                  { "id": "{{subId}}", "points": 100, "question": { "id": "{{Guid.NewGuid()}}",
                    "answers": [ { "id": "{{correct}}", "isCorrect": true }, { "id": "{{wrong}}", "isCorrect": false } ] } }
                ] }
              ]
            }
            """;
            var client = BuildClient(new StubHandler(HttpStatusCode.OK, json));

            var content = await client.GetSubStageContentAsync(missionId, subId);

            content.Should().NotBeNull();
            content!.Type.Should().Be(SubmissionType.Trivia);
            content.Points.Should().Be(100);
            content.CorrectAnswerIds.Should().ContainSingle().Which.Should().Be(correct);
            content.SecretCode.Should().BeNull();
        }

        [Fact]
        public async Task GetSubStageContentAsync_ForTreasureHunt_ShouldReturnSecretCode()
        {
            var missionId = Guid.NewGuid();
            var subId = Guid.NewGuid();
            var json = $$"""
            {
              "id": "{{missionId}}",
              "stages": [
                { "id": "{{Guid.NewGuid()}}", "subStages": [
                  { "id": "{{subId}}", "points": 50, "objective": "Encuentra la llave", "secretCode": "ABC123" }
                ] }
              ]
            }
            """;
            var client = BuildClient(new StubHandler(HttpStatusCode.OK, json));

            var content = await client.GetSubStageContentAsync(missionId, subId);

            content.Should().NotBeNull();
            content!.Type.Should().Be(SubmissionType.TreasureHunt);
            content.Points.Should().Be(50);
            content.SecretCode.Should().Be("ABC123");
            content.CorrectAnswerIds.Should().BeEmpty();
        }

        [Fact]
        public async Task GetSubStageContentAsync_WhenMissionNotFound_ShouldReturnNull()
        {
            var client = BuildClient(new StubHandler(HttpStatusCode.NotFound, ""));

            var content = await client.GetSubStageContentAsync(Guid.NewGuid(), Guid.NewGuid());

            content.Should().BeNull();
        }

        [Fact]
        public async Task GetSubStageContentAsync_WhenSubStageMissing_ShouldReturnNull()
        {
            var missionId = Guid.NewGuid();
            var json = $$"""
            { "id": "{{missionId}}", "stages": [ { "id": "{{Guid.NewGuid()}}", "subStages": [] } ] }
            """;
            var client = BuildClient(new StubHandler(HttpStatusCode.OK, json));

            var content = await client.GetSubStageContentAsync(missionId, Guid.NewGuid());

            content.Should().BeNull();
        }

        [Fact]
        public async Task GetPlayerSubStagesAsync_ShouldReturnSanitizedContentWithoutAnswers()
        {
            var missionId = Guid.NewGuid();
            var triviaId = Guid.NewGuid();
            var treasureId = Guid.NewGuid();
            var json = $$"""
            {
              "id": "{{missionId}}",
              "stages": [
                { "id": "{{Guid.NewGuid()}}", "subStages": [
                  { "id": "{{triviaId}}", "title": "Capital", "points": 100, "question": { "id": "{{Guid.NewGuid()}}", "text": "¿Capital?",
                    "answers": [ { "id": "{{Guid.NewGuid()}}", "text": "París", "isCorrect": true }, { "id": "{{Guid.NewGuid()}}", "text": "Londres", "isCorrect": false } ] } },
                  { "id": "{{treasureId}}", "title": "Codigo", "points": 50, "objective": "Encuentra la placa", "secretCode": "UMBRAL" }
                ] }
              ]
            }
            """;
            var client = BuildClient(new StubHandler(HttpStatusCode.OK, json));

            var subStages = await client.GetPlayerSubStagesAsync(missionId, new[] { triviaId, treasureId });

            subStages.Should().HaveCount(2);

            var trivia = subStages.First(s => s.Id == triviaId);
            trivia.Type.Should().Be(SubmissionType.Trivia);
            trivia.Question!.Text.Should().Be("¿Capital?");
            trivia.Question.Answers.Should().HaveCount(2);
            trivia.Question.Answers.Should().Contain(a => a.Text == "París");

            var treasure = subStages.First(s => s.Id == treasureId);
            treasure.Type.Should().Be(SubmissionType.TreasureHunt);
            treasure.Objective.Should().Be("Encuentra la placa");
            // PlayerSubStageDto no expone isCorrect ni secretCode: la sanitización es estructural.
        }

        [Fact]
        public async Task GetSubStageContentAsync_ShouldAuthenticateWithServiceToken()
        {
            var missionId = Guid.NewGuid();
            var subId = Guid.NewGuid();
            var json = $$"""
            { "id": "{{missionId}}", "stages": [ { "id": "{{Guid.NewGuid()}}", "subStages": [ { "id": "{{subId}}", "points": 10, "secretCode": "X" } ] } ] }
            """;
            var handler = new StubHandler(HttpStatusCode.OK, json);
            var client = BuildClient(handler);

            await client.GetSubStageContentAsync(missionId, subId);

            handler.LastRequest!.Headers.Authorization!.Scheme.Should().Be("Bearer");
            handler.LastRequest.Headers.Authorization.Parameter.Should().Be("service-token");
        }
    }
}
