using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.Abstractions;
using ScoringService.Application.Commands;
using ScoringService.Application.DTOs;
using ScoringService.Application.IntegrationEvents;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Events;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.Scoring;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Application.Handlers
{
    /// <summary>
    /// Registra la evidencia de un equipo (RF-08/09) y la autovalida contra el contenido de
    /// missionService (RF-10). Si el contenido no está disponible, la evidencia queda pendiente
    /// para validación manual del operador. Cumple RB-03 (sesión activa) y RB-05 (asociación única).
    /// </summary>
    public sealed class SubmitEvidenceCommandHandler : IRequestHandler<SubmitEvidenceCommand, SubmitEvidenceResultDto>
    {
        private readonly IScoringRepository _scoringRepository;
        private readonly ISessionMonitorRepository _sessionMonitorRepository;
        private readonly IMissionContentClient _missionContentClient;
        private readonly IScoreBroadcaster _scoreBroadcaster;
        private readonly IEventBus _eventBus;
        private readonly IScoringNotificationService _notificationService;
        private readonly IScoringStrategyResolver _scoringStrategyResolver;

        public SubmitEvidenceCommandHandler(
            IScoringRepository scoringRepository,
            ISessionMonitorRepository sessionMonitorRepository,
            IMissionContentClient missionContentClient,
            IScoreBroadcaster scoreBroadcaster,
            IEventBus eventBus,
            IScoringNotificationService notificationService,
            IScoringStrategyResolver scoringStrategyResolver)
        {
            _scoringRepository = scoringRepository;
            _sessionMonitorRepository = sessionMonitorRepository;
            _missionContentClient = missionContentClient;
            _scoreBroadcaster = scoreBroadcaster;
            _eventBus = eventBus;
            _notificationService = notificationService;
            _scoringStrategyResolver = scoringStrategyResolver;
        }

        public async Task<SubmitEvidenceResultDto> Handle(SubmitEvidenceCommand request, CancellationToken cancellationToken)
        {
            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            // RB-03: solo se aceptan evidencias con la sesión activa.
            if (!monitor.AcceptsEvidence())
                throw new SessionNotAcceptingEvidenceException(request.SessionId);

            // RB-05: el equipo debe pertenecer a la sesión.
            if (!monitor.HasTeam(request.TeamId))
                throw new TeamNotInSessionException(request.TeamId, request.SessionId);

            // Trivia: un solo intento (aciertes o falles, se cierra). Búsqueda del tesoro:
            // reintentos hasta acertar, y una vez resuelta no se puede volver a puntuar.
            if (request.Type == SubmissionType.Trivia)
            {
                if (await _scoringRepository.HasAnySubmissionAsync(
                        request.SessionId, request.TeamId, request.SubStageId, cancellationToken))
                    throw new TriviaAlreadyAnsweredException(request.SubStageId);
            }
            else if (await _scoringRepository.HasSolvedSubStageAsync(
                         request.SessionId, request.TeamId, request.SubStageId, cancellationToken))
            {
                throw new SubStageAlreadySolvedException(request.SubStageId);
            }

            var now = DateTime.UtcNow;
            var submission = EvidenceSubmission.Create(
                Guid.NewGuid(), request.SessionId, request.TeamId, request.SubStageId,
                request.Type, request.Payload, now);

            var content = await _missionContentClient.GetSubStageContentAsync(
                monitor.MissionId, request.SubStageId, cancellationToken);

            ScoreEntry? scoreEntry = null;
            if (content is not null)
            {
                if (IsCorrect(request.Type, request.Payload, content))
                {
                    // El puntaje otorgado varía según la dificultad de la misión (patrón Strategy).
                    var award = _scoringStrategyResolver.Resolve(content.Difficulty).CalculateAward(content.Points);
                    submission.MarkCorrect(award.Points, now);
                    scoreEntry = ScoreEntry.ForEvidence(
                        Guid.NewGuid(), request.SessionId, request.TeamId,
                        award, submission.Id, now);
                }
                else
                {
                    submission.MarkIncorrect(now);
                }
            }

            await _scoringRepository.AddSubmissionAsync(submission, cancellationToken);
            if (scoreEntry is not null)
                await _scoringRepository.AddScoreEntryAsync(scoreEntry, cancellationToken);

            await _scoringRepository.AddSessionEventAsync(
                SessionEvent.Record(
                    Guid.NewGuid(), request.SessionId, "EvidenceSubmitted",
                    $"Equipo {request.TeamId} envió evidencia para la etapa {request.SubStageId} (estado: {submission.Status}).",
                    now),
                cancellationToken);

            await _scoringRepository.SaveChangesAsync(cancellationToken);

            await _eventBus.PublishAsync(
                new EvidenceSubmittedIntegrationEvent(
                    submission.Id, request.SessionId, request.TeamId, request.SubStageId,
                    submission.Status.ToString(), submission.SubmittedAt),
                cancellationToken);

            // Consumir los eventos de dominio registrados por la entidad (ddd-modeling §4).
            foreach (var domainEvent in submission.DomainEvents)
            {
                if (domainEvent is EvidenceValidatedDomainEvent validated)
                {
                    await _notificationService.NotifyEvidenceValidatedAsync(
                        validated.SessionId, validated.TeamId, validated.SubmissionId,
                        validated.IsCorrect, validated.AwardedPoints, cancellationToken);
                }
            }
            submission.ClearDomainEvents();

            int teamTotal;
            if (submission.Status == SubmissionStatus.Correct)
            {
                // Recalcula, difunde ranking (RF-12) y publica el recálculo (RF-14).
                teamTotal = await _scoreBroadcaster.BroadcastAsync(
                    request.SessionId, request.TeamId, submission.AwardedPoints ?? 0, now, cancellationToken);
            }
            else
            {
                var teamEntries = await _scoringRepository.GetScoreEntriesByTeamAsync(
                    request.SessionId, request.TeamId, cancellationToken);
                teamTotal = teamEntries.Sum(e => e.Value.Points);
            }

            return new SubmitEvidenceResultDto(
                submission.Id, submission.Status.ToString(), submission.AwardedPoints ?? 0, teamTotal);
        }

        private static bool IsCorrect(SubmissionType type, string payload, SubStageContent content)
        {
            return type switch
            {
                SubmissionType.Trivia =>
                    Guid.TryParse(payload, out var answerId) && content.CorrectAnswerIds.Contains(answerId),
                SubmissionType.TreasureHunt =>
                    !string.IsNullOrWhiteSpace(content.SecretCode) &&
                    string.Equals(Normalize(payload), content.SecretCode, StringComparison.Ordinal),
                _ => false
            };
        }

        // Misma normalización que aplica missionService al guardar el código secreto.
        private static string Normalize(string value) => value.Trim().ToUpperInvariant();
    }
}
