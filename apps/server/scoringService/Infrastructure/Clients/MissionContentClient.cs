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
    /// autovalidar una evidencia. Autentica con la identidad de servicio del scoring
    /// (client credentials), no con el token del jugador, para no exponerle las respuestas.
    /// </summary>
    public sealed class MissionContentClient : IMissionContentClient
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceTokenProvider _serviceTokenProvider;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public MissionContentClient(
            HttpClient httpClient,
            IServiceTokenProvider serviceTokenProvider,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _serviceTokenProvider = serviceTokenProvider;
            _baseUrl = configuration["MissionService:BaseUrl"] ?? "http://missionservice:5022";
        }

        public async Task<SubStageContent?> GetSubStageContentAsync(
            Guid missionId, Guid subStageId, CancellationToken cancellationToken = default)
        {
            var mission = await FetchMissionAsync(missionId, cancellationToken);
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
                subStage.SecretCode,
                MissionDifficultyExtensions.FromString(mission.DifficultyLevel));
        }

        public async Task<IReadOnlyList<PlayerSubStageDto>> GetPlayerSubStagesAsync(
            Guid missionId, IReadOnlyCollection<Guid> subStageIds, CancellationToken cancellationToken = default)
        {
            var empty = new List<PlayerSubStageDto>();
            if (subStageIds.Count == 0)
                return empty;

            var mission = await FetchMissionAsync(missionId, cancellationToken);
            if (mission?.Stages is null)
                return empty;

            var wanted = new HashSet<Guid>(subStageIds);
            return mission.Stages
                .Where(s => s.SubStages is not null)
                .SelectMany(s => s.SubStages!)
                .Where(ss => wanted.Contains(ss.Id))
                .Select(ToPlayerSubStage)
                .ToList();
        }

        private static PlayerSubStageDto ToPlayerSubStage(SubStageResponse subStage)
        {
            var isTrivia = subStage.Question is not null;

            // Se omiten deliberadamente isCorrect y secretCode: el jugador no debe verlos.
            PlayerQuestionDto? question = null;
            if (subStage.Question is not null)
            {
                var answers = (subStage.Question.Answers ?? new List<AnswerResponse>())
                    .Select(a => new PlayerAnswerDto(a.Id, a.Text))
                    .ToList();
                question = new PlayerQuestionDto(subStage.Question.Id, subStage.Question.Text, answers);
            }

            return new PlayerSubStageDto(
                subStage.Id,
                subStage.Title ?? string.Empty,
                subStage.Points,
                isTrivia ? SubmissionType.Trivia : SubmissionType.TreasureHunt,
                question,
                subStage.Objective,
                Latitude: subStage.Latitude,
                Longitude: subStage.Longitude,
                SearchRadiusMeters: subStage.SearchRadiusMeters);
        }

        private async Task<MissionResponse?> FetchMissionAsync(Guid missionId, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/api/missions/{missionId}");

            var serviceToken = await _serviceTokenProvider.GetTokenAsync(cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", serviceToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<MissionResponse>(body, SerializerOptions);
        }

        // Proyección local del JSON devuelto por missionService (GET /api/missions/{id}).
        private sealed record MissionResponse(Guid Id, string? DifficultyLevel, List<StageResponse>? Stages);
        private sealed record StageResponse(Guid Id, List<SubStageResponse>? SubStages);
        private sealed record SubStageResponse(Guid Id, string? Title, int Points, QuestionResponse? Question, string? Objective, string? SecretCode, double? Latitude, double? Longitude, int? SearchRadiusMeters);
        private sealed record QuestionResponse(Guid Id, string Text, List<AnswerResponse>? Answers);
        private sealed record AnswerResponse(Guid Id, string Text, bool IsCorrect);
    }
}
