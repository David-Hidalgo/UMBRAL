using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.DTOs;
using MissionService.Application.Queries;
using MissionService.Domain.RepositoryInterfaces;

namespace MissionService.Application.Handlers
{
    /// <summary>
    /// Handles the read-side GetMissionsQuery.
    /// Delegates filtering to the repository and maps domain entities to summary DTOs.
    /// Does not modify state — no SaveChanges call.
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
                m.Status.Value,
                m.Stages.Count
            ));
        }
    }
}
