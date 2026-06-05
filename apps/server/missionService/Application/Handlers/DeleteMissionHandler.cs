using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;

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

            // Guard: Cannot delete mission if it or any of its stages have active sessions
            if (mission.Status.IsInUse || mission.Stages.Any(s => s.Status.IsInUse))
            {
                throw new MissionHasActiveSessionsException(request.Id);
            }

            _repository.Remove(mission);
            await _repository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
