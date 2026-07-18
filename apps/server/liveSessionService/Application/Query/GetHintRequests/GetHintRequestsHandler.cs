using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Queries;
using LiveSessionService.Domain.Enums;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;

namespace LiveSessionService.Application.Handlers
{
    public class GetHintRequestsHandler : IRequestHandler<GetHintRequestsQuery, IReadOnlyList<HintRequestDto>>
    {
        private readonly ILiveSessionRepository _repository;

        public GetHintRequestsHandler(ILiveSessionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IReadOnlyList<HintRequestDto>> Handle(GetHintRequestsQuery request, CancellationToken cancellationToken)
        {
            var session = await _repository.GetByIdAsync(request.SessionId, cancellationToken);
            if (session == null)
            {
                throw new BusinessException("La sesión de juego especificada no existe.");
            }

            var teamNames = session.Teams.ToDictionary(t => t.Id, t => t.Name);

            var query = session.HintRequests.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (!Enum.TryParse<HintRequestStatus>(request.Status, true, out var statusFilter))
                    throw new BusinessException("El estado indicado no es válido. Use 'Pendiente' o 'Resuelta'.");

                query = query.Where(r => r.Status == statusFilter);
            }

            return query
                .OrderBy(r => r.RequestedAt)
                .Select(r => new HintRequestDto(
                    r.Id,
                    r.TeamId,
                    teamNames.TryGetValue(r.TeamId, out var name) ? name : string.Empty,
                    r.SubStageId,
                    r.Status.ToString(),
                    r.RequestedAt,
                    r.ResolvedAt
                ))
                .ToList();
        }
    }
}
