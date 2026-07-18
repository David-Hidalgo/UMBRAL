using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Exceptions;
using MissionService.Domain.Repositories;

namespace MissionService.Application.Handlers
{
    public sealed class DeleteMissionHandler : IRequestHandler<DeleteMissionCommand, Unit>
    {
        private readonly IMissionRepository _repository;

        public DeleteMissionHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(DeleteMissionCommand request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (mission == null)
            {
                throw new MissionNotFoundException(request.Id);
            }

            // Guard: No se puede eliminar la misión si ella o alguna de sus etapas tienen sesiones activas
            if (mission.IsInUse)
            {
                throw new MissionHasActiveSessionsException(request.Id);
            }

            _repository.Remove(mission);
            await _repository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
