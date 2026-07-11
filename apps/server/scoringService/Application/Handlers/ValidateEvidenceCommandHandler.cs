using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using ScoringService.Application.Abstractions;
using ScoringService.Application.Commands;
using ScoringService.Application.DTOs;
using ScoringService.Domain.Entities;
using ScoringService.Domain.Enums;
using ScoringService.Domain.Events;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.ValueObjects;

namespace ScoringService.Application.Handlers
{
    /// <summary>
    /// El operador valida manualmente una evidencia pendiente (RF-10). Al declararla correcta se
    /// otorga el puntaje base de la etapa (obtenido de missionService) y se recalcula el ranking.
    /// </summary>
    public sealed class ValidateEvidenceCommandHandler : IRequestHandler<ValidateEvidenceCommand, SubmitEvidenceResultDto>
    {
        private readonly IScoringRepository _scoringRepository;
        private readonly ISessionMonitorRepository _sessionMonitorRepository;
        private readonly IMissionContentClient _missionContentClient;
        private readonly IScoreBroadcaster _scoreBroadcaster;
        private readonly IScoringNotificationService _notificationService;

        public ValidateEvidenceCommandHandler(
            IScoringRepository scoringRepository,
            ISessionMonitorRepository sessionMonitorRepository,
            IMissionContentClient missionContentClient,
            IScoreBroadcaster scoreBroadcaster,
            IScoringNotificationService notificationService)
        {
            _scoringRepository = scoringRepository;
            _sessionMonitorRepository = sessionMonitorRepository;
            _missionContentClient = missionContentClient;
            _scoreBroadcaster = scoreBroadcaster;
            _notificationService = notificationService;
        }

        public async Task<SubmitEvidenceResultDto> Handle(ValidateEvidenceCommand request, CancellationToken cancellationToken)
        {
            var submission = await _scoringRepository.GetSubmissionByIdAsync(request.SubmissionId, cancellationToken)
                ?? throw new SubmissionNotFoundException(request.SubmissionId);

            if (submission.SessionId != request.SessionId)
                throw new SubmissionNotFoundException(request.SubmissionId);

            var monitor = await _sessionMonitorRepository.GetBySessionIdAsync(request.SessionId, cancellationToken)
                ?? throw new SessionMonitorNotFoundException(request.SessionId);

            var now = DateTime.UtcNow;
            ScoreEntry? scoreEntry = null;

            if (request.IsCorrect)
            {
                var content = await _missionContentClient.GetSubStageContentAsync(
                    monitor.MissionId, submission.SubStageId, cancellationToken)
                    ?? throw new InvalidSubmissionException(
                        "No se pudo obtener el puntaje de la etapa desde el servicio de misiones.");

                submission.MarkCorrect(content.Points, now);
                scoreEntry = ScoreEntry.ForEvidence(
                    Guid.NewGuid(), submission.SessionId, submission.TeamId,
                    ScoreValue.Award(content.Points), submission.Id, now);
            }
            else
            {
                submission.MarkIncorrect(now);
            }

            if (scoreEntry is not null)
                await _scoringRepository.AddScoreEntryAsync(scoreEntry, cancellationToken);

            await _scoringRepository.AddSessionEventAsync(
                SessionEvent.Record(
                    Guid.NewGuid(), submission.SessionId, "EvidenceValidated",
                    $"El operador validó la evidencia {submission.Id} como {submission.Status}.",
                    now, request.Actor),
                cancellationToken);

            await _scoringRepository.SaveChangesAsync(cancellationToken);

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
                teamTotal = await _scoreBroadcaster.BroadcastAsync(
                    submission.SessionId, submission.TeamId, submission.AwardedPoints ?? 0, now, cancellationToken);
            }
            else
            {
                var teamEntries = await _scoringRepository.GetScoreEntriesByTeamAsync(
                    submission.SessionId, submission.TeamId, cancellationToken);
                teamTotal = teamEntries.Sum(e => e.Value.Points);
            }

            return new SubmitEvidenceResultDto(
                submission.Id, submission.Status.ToString(), submission.AwardedPoints ?? 0, teamTotal);
        }
    }
}
