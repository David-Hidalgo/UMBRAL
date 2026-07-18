using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.Repositories;

namespace MissionService.Application.Handlers
{
    public sealed class UpdateMissionHandler : IRequestHandler<UpdateMissionCommand, Unit>
    {
        private readonly IMissionRepository _repository;

        public UpdateMissionHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(UpdateMissionCommand request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.Id, cancellationToken);
            if (mission == null)
            {
                throw new MissionNotFoundException(request.Id);
            }

            // If name is changing, check uniqueness excluding current mission
            if (request.Name != null && !request.Name.Trim().Equals(mission.Name, StringComparison.OrdinalIgnoreCase))
            {
                var trimmedName = request.Name.Trim();
                if (await _repository.ExistsByNameExcludingIdAsync(trimmedName, request.Id, cancellationToken))
                {
                    throw new DuplicateMissionNameException(trimmedName);
                }
            }

            DifficultyLevel? difficulty = null;
            if (request.DifficultyLevel != null)
            {
                difficulty = DifficultyLevelExtensions.FromString(request.DifficultyLevel);
            }

            mission.UpdateInfo(request.Name, request.Description, difficulty, request.MaxTimeMinutes);

            if (request.State != null)
            {
                if (request.State.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    mission.Activate();
                }
                else if (request.State.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
                {
                    mission.Deactivate();
                }
                else
                {
                    throw new ArgumentException($"Invalid mission state: '{request.State}'", nameof(request.State));
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
