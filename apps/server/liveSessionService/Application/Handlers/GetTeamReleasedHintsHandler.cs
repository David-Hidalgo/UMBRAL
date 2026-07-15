using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class GetTeamReleasedHintsHandler : IRequestHandler<GetTeamReleasedHintsQuery, IReadOnlyList<ReleasedHintDto>>
    {
        private readonly ILiveSessionRepository _repository;

        public GetTeamReleasedHintsHandler(ILiveSessionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<ReleasedHintDto>> Handle(GetTeamReleasedHintsQuery request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            // Autorización: las pistas de un equipo son privadas. Un admin/operator puede verlas
            // (p. ej. para no ofrecer repetidas al otorgar), pero un jugador solo puede consultar
            // las de su propio equipo. Sin esto, cualquier autenticado que conozca el teamId leería
            // las pistas de un rival (mismo control que ya aplica el hub en JoinTeamGroup).
            if (!request.IsPrivileged)
            {
                var team = session.Teams.FirstOrDefault(t => t.Id == request.TeamId);
                var isMember = team?.Players.Any(p => p.KeycloakUserId == request.RequestingUserId) ?? false;
                if (!isMember)
                {
                    throw new BusinessException("No tienes permiso para consultar las pistas de este equipo.");
                }
            }

            return session.ReleasedHints
                .Where(h => h.TeamId == request.TeamId)
                .OrderBy(h => h.ReleasedAt)
                .Select(h => new ReleasedHintDto(
                    h.Id,
                    h.TeamId,
                    h.SubStageId,
                    h.ClueId,
                    h.ClueText,
                    h.ClueOrder,
                    h.ReleasedAt
                ))
                .ToList();
        }
    }
}
