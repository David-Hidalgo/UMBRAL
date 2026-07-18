using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.DTOs;
using MissionService.Application.Queries;
using MissionService.Domain.Repositories;

namespace MissionService.Application.Handlers
{
    /// <summary>
    /// Maneja el lado de lectura de GetMissionsQuery.
    /// Delega el filtrado al repositorio y mapea las entidades de dominio a DTOs de resumen.
    /// No modifica el estado — sin llamadas a SaveChanges.
    /// </summary>
    public sealed class GetMissionsHandler : IRequestHandler<GetMissionsQuery, IEnumerable<MissionSummaryDto>>
    {
        private readonly IMissionRepository _repository;

        public GetMissionsHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<MissionSummaryDto>> Handle(
            GetMissionsQuery request,
            CancellationToken cancellationToken)
        {
            var missions = await _repository.GetAllAsync(
                request.SearchTerm,
                request.Status,
                cancellationToken);

            return missions.Select(m => new MissionSummaryDto(
                m.Id,
                m.Name,
                m.Description,
                m.State.ToString(),
                m.Stages.SelectMany(s => s.SubStages).Sum(ss => ss.Status.ActiveSessionCount),
                m.DifficultyLevel.ToString(),
                m.MaxTimeMinutes,
                m.Stages.Count
            ));
        }
    }
}
