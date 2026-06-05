using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MissionService.Application.Commands;
using MissionService.Domain.Entities;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.RepositoryInterfaces;

namespace MissionService.Application.Handlers
{
    public class CreateMissionHandler : IRequestHandler<CreateMissionCommand, Guid>
    {
        private readonly IMissionRepository _repository;

        public CreateMissionHandler(IMissionRepository repository)
        {
            _repository = repository;
        }

        public async Task<Guid> Handle(CreateMissionCommand request, CancellationToken cancellationToken)
        {
            // 1. Validar unicidad del nombre contra la BD
            if (await _repository.ExistsByNameAsync(request.Name, cancellationToken))
            {
                throw new DuplicateMissionNameException(request.Name);
            }

            // 2. Mapear jerarquía a entidades de dominio construidas mediante Factory Methods
            var domainStages = new List<Stage>();

            foreach (var sc in request.Stages)
            {
                var domainSubStages = new List<SubStage>();

                foreach (var ssc in sc.SubStages)
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

                var stage = Stage.Create(Guid.NewGuid(), sc.Name, sc.Description, sc.Order, domainSubStages);
                domainStages.Add(stage);
            }

            // 3. Parsear dificultad
            var difficulty = DifficultyLevelExtensions.FromString(request.DifficultyLevel);

            // 4. Crear el Agregado de Misión
            var mission = Mission.Create(Guid.NewGuid(), request.Name, request.Description, difficulty, request.MaxTimeMinutes, domainStages);

            // 5. Persistir a la base de datos
            await _repository.AddAsync(mission, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return mission.Id;
        }
    }
}
