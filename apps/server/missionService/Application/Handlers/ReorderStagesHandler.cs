using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;

namespace MissionService.Application.Handlers
{
    public sealed class ReorderStagesHandler : IRequestHandler<ReorderStagesCommand, Unit>
    {
        private readonly IMissionRepository _repository;

        public ReorderStagesHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(ReorderStagesCommand request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
            if (mission == null)
            {
                throw new MissionNotFoundException(request.MissionId);
            }

            mission.ReorderStages(request.StageIdsInOrder);

            await _repository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
