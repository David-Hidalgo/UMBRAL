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
    public sealed class UpdateStageHandler : IRequestHandler<UpdateStageCommand, Unit>
    {
        private readonly IMissionRepository _repository;

        public UpdateStageHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Unit> Handle(UpdateStageCommand request, CancellationToken cancellationToken)
        {
            var mission = await _repository.GetByIdAsync(request.MissionId, cancellationToken);
            if (mission == null)
            {
                throw new MissionNotFoundException(request.MissionId);
            }

            var stage = mission.Stages.FirstOrDefault(s => s.Id == request.StageId);
            if (stage == null)
            {
                throw new StageNotFoundException(request.StageId);
            }

            stage.Update(request.Name, request.Description);

            // 1. Eliminar las subetapas (substages) que no estén presentes en la lista de actualización entrante
            var incomingIds = request.SubStages.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToHashSet();
            var subStagesToRemove = stage.SubStages.Where(s => !incomingIds.Contains(s.Id)).ToList();
            foreach (var subStageToRemove in subStagesToRemove)
            {
                stage.RemoveSubStage(subStageToRemove.Id);
            }

            // 2. Procesar las subetapas entrantes (actualizar o agregar)
            foreach (var ssc in request.SubStages)
            {
                SubStage? existingSubStage = null;
                if (ssc.Id.HasValue)
                {
                    existingSubStage = stage.SubStages.FirstOrDefault(s => s.Id == ssc.Id.Value);
                }

                var domainClues = new List<Clue>();
                if (ssc.Clues != null)
                {
                    foreach (var cc in ssc.Clues)
                    {
                        var clueId = cc.Id ?? Guid.NewGuid();
                        domainClues.Add(Clue.Create(clueId, cc.Text, cc.Order));
                    }
                }

                if (existingSubStage != null)
                {
                    // Actualizar existente
                    existingSubStage.Update(ssc.Title, ssc.Order, ssc.Points);
                    existingSubStage.UpdateClues(domainClues);

                    if (existingSubStage is TriviaSubStage triviaSubStage)
                    {
                        if (ssc.Question == null)
                            throw new ArgumentException("Trivia sub-stage must have a question.");

                        var domainAnswers = (ssc.Question.Answers ?? [])
                            .Select(ac => Answer.Create(ac.Id ?? Guid.NewGuid(), ac.Text, ac.IsCorrect))
                            .ToList();

                        triviaSubStage.UpdateQuestion(ssc.Question.Text, domainAnswers);
                    }
                    else if (existingSubStage is TreasureHuntSubStage treasureHuntSubStage)
                    {
                        treasureHuntSubStage.UpdateDetails(ssc.Objective ?? string.Empty, ssc.SecretCode ?? string.Empty);
                    }
                }
                else
                {
                    // Agregar nueva
                    var newSubStageId = ssc.Id ?? Guid.NewGuid();
                    if (ssc.Type.Equals("Trivia", StringComparison.OrdinalIgnoreCase))
                    {
                        if (ssc.Question == null)
                            throw new ArgumentException("Trivia sub-stage must have a question.");

                        var domainAnswers = (ssc.Question.Answers ?? [])
                            .Select(ac => Answer.Create(ac.Id ?? Guid.NewGuid(), ac.Text, ac.IsCorrect))
                            .ToList();

                        var question = Question.Create(ssc.Question.Id ?? Guid.NewGuid(), ssc.Question.Text, domainAnswers);

                        var triviaSubStage = TriviaSubStage.Create(
                            newSubStageId,
                            ssc.Title,
                            ssc.Order,
                            ssc.Points,
                            question,
                            domainClues
                        );
                        stage.AddSubStage(triviaSubStage);
                    }
                    else if (ssc.Type.Equals("TreasureHunt", StringComparison.OrdinalIgnoreCase))
                    {
                        var treasureHuntSubStage = TreasureHuntSubStage.Create(
                            newSubStageId,
                            ssc.Title,
                            ssc.Order,
                            ssc.Points,
                            ssc.Objective ?? string.Empty,
                            ssc.SecretCode ?? string.Empty,
                            domainClues
                        );
                        stage.AddSubStage(treasureHuntSubStage);
                    }
                }
            }

            await _repository.SaveChangesAsync(cancellationToken);

            return Unit.Value;
        }
    }
}
