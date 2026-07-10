using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ScoringService.Application.Abstractions;
using ScoringService.Application.DTOs;
using ScoringService.Domain.Enums;

namespace ScoringService.Infrastructure.Clients
{
    /// <summary>
    /// Adaptador HTTP hacia missionService. Obtiene el detalle de la misión y proyecta el
    /// contenido de la etapa solicitada (puntaje, respuestas correctas, código secreto) para
    /// autovalidar una evidencia. Reenvía el JWT del llamador.
    /// </summary>
    public sealed class MissionContentClient : IMissionContentClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public MissionContentClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["MissionService:BaseUrl"] ?? "http://missionservice:5022";
        }

        public async Task<SubStageContent?> GetSubStageContentAsync(
            Guid missionId, Guid subStageId, string jwtToken, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/api/missions/{missionId}");

            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                var cleanToken = jwtToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? jwtToken.Substring(7).Trim()
                    : jwtToken.Trim();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanToken);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var mission = JsonSerializer.Deserialize<MissionResponse>(body, SerializerOptions);
            if (mission?.Stages is null)
                return null;

            var subStage = mission.Stages
                .Where(s => s.SubStages is not null)
                .SelectMany(s => s.SubStages!)
                .FirstOrDefault(ss => ss.Id == subStageId);

            if (subStage is null)
                return null;

            var isTrivia = subStage.Question is not null;
            var correctAnswerIds = subStage.Question?.Answers?
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .ToList() ?? new List<Guid>();

            return new SubStageContent(
                subStage.Id,
                isTrivia ? SubmissionType.Trivia : SubmissionType.TreasureHunt,
                subStage.Points,
                correctAnswerIds,
                subStage.SecretCode);
        }

        // Proyección local del JSON devuelto por missionService (GET /api/missions/{id}).
        private sealed record MissionResponse(Guid Id, List<StageResponse>? Stages);
        private sealed record StageResponse(Guid Id, List<SubStageResponse>? SubStages);
        private sealed record SubStageResponse(Guid Id, int Points, QuestionResponse? Question, string? Objective, string? SecretCode);
        private sealed record QuestionResponse(Guid Id, List<AnswerResponse>? Answers);
        private sealed record AnswerResponse(Guid Id, bool IsCorrect);
    }
}
