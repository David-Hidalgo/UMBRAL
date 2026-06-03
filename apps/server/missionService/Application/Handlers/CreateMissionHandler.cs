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
                    if (ssc.Type.Equals("Trivia", StringComparison.OrdinalIgnoreCase))
                    {
                        var domainQuestions = new List<Question>();
                        
                        if (ssc.Questions != null)
                        {
                            foreach (var qc in ssc.Questions)
                            {
                                var domainAnswers = qc.Answers
                                    .Select(ac => Answer.Create(Guid.NewGuid(), ac.Text, ac.IsCorrect))
                                    .ToList();

                                var question = Question.Create(Guid.NewGuid(), qc.Text, qc.Points, domainAnswers);
                                domainQuestions.Add(question);
                            }
                        }

                        var triviaSubStage = TriviaSubStage.Create(Guid.NewGuid(), ssc.Title, ssc.Order, domainQuestions);
                        domainSubStages.Add(triviaSubStage);
                    }
                    else if (ssc.Type.Equals("TreasureHunt", StringComparison.OrdinalIgnoreCase))
                    {
                        var domainClues = new List<Clue>();

                        if (ssc.Clues != null)
                        {
                            foreach (var cc in ssc.Clues)
                            {
                                var clue = Clue.Create(Guid.NewGuid(), cc.Text, cc.Order, cc.PenaltyPoints);
                                domainClues.Add(clue);
                            }
                        }

                        var treasureHuntSubStage = TreasureHuntSubStage.Create(
                            Guid.NewGuid(), 
                            ssc.Title, 
                            ssc.Order, 
                            ssc.Objective ?? string.Empty, 
                            domainClues
                        );
                        domainSubStages.Add(treasureHuntSubStage);
                    }
                }

                var stage = Stage.Create(Guid.NewGuid(), sc.Name, sc.Description, sc.Order, domainSubStages);
                domainStages.Add(stage);
            }

            // 3. Crear el Agregado de Misión
            var mission = Mission.Create(Guid.NewGuid(), request.Name, request.Description, domainStages);

            // 4. Persistir a la base de datos
            await _repository.AddAsync(mission, cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);

            return mission.Id;
        }
    }
}
