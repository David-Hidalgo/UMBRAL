using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Entities;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;

namespace MissionService.Application.Handlers
{
    public sealed class AddStageHandler : IRequestHandler<AddStageCommand, Guid>
    {
        private readonly IMissionRepository _repository;

        public AddStageHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(AddStageCommand request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
            if (mission == null)
            {
                throw new MissionNotFoundException(request.MissionId);
            }

            var domainSubStages = new List<SubStage>();

            foreach (var ssc in request.SubStages)
            {
                var domainClues = new List<Clue>();
                if (ssc.Clues != null)
                {
                    foreach (var cc in ssc.Clues)
                    {
                        var clue = Clue.Create(Guid.NewGuid(), cc.Text, cc.Order);
                        domainClues.Add(clue);
                    }
                }

                if (ssc.Type.Equals("Trivia", StringComparison.OrdinalIgnoreCase))
                {
                    if (ssc.Question == null)
                        throw new ArgumentException("Trivia sub-stage must have a question.");

                    var domainAnswers = (ssc.Question.Answers ?? [])
                        .Select(ac => Answer.Create(Guid.NewGuid(), ac.Text, ac.IsCorrect))
                        .ToList();

                    var question = Question.Create(Guid.NewGuid(), ssc.Question.Text, domainAnswers);

                    var triviaSubStage = TriviaSubStage.Create(
                        Guid.NewGuid(),
                        ssc.Title,
                        ssc.Order,
                        ssc.Points,
                        question,
                        domainClues
                    );
                    domainSubStages.Add(triviaSubStage);
                }
                else if (ssc.Type.Equals("TreasureHunt", StringComparison.OrdinalIgnoreCase))
                {
                    var treasureHuntSubStage = TreasureHuntSubStage.Create(
                        Guid.NewGuid(),
                        ssc.Title,
                        ssc.Order,
                        ssc.Points,
                        ssc.Objective ?? string.Empty,
                        ssc.SecretCode ?? string.Empty,
                        domainClues
                    );
                    domainSubStages.Add(treasureHuntSubStage);
                }
            }

            var stage = Stage.Create(Guid.NewGuid(), request.Name, request.Description, request.Order, domainSubStages);
            
            mission.AddStage(stage);

            await _repository.SaveChangesAsync(cancellationToken);

            return stage.Id;
        }
    }
}
