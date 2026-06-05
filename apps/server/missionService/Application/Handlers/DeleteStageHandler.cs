using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;

namespace MissionService.Application.Handlers
{
    public sealed class DeleteStageHandler : IRequestHandler<DeleteStageCommand, Unit>
    {
        private readonly IMissionRepository _repository;

        public DeleteStageHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(DeleteStageCommand request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
            if (mission == null)
            {
                throw new MissionNotFoundException(request.MissionId);
            }

            mission.RemoveStage(request.StageId);

            await _repository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
