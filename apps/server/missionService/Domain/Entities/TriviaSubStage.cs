using System;
using System.Collections.Generic;

namespace MissionService.Domain.Entities
{
    public class TriviaSubStage : SubStage
    {
        public Question Question { get; private set; } = null!;

        private TriviaSubStage() : base() { }

        public static TriviaSubStage Create(Guid id, string title, int order, int points, Question question, List<Clue>? clues = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Substage ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Substage title cannot be empty.", nameof(title));
            if (points < 0)
                throw new ArgumentException("Substage points cannot be negative.", nameof(points));
            if (question == null)
                throw new ArgumentNullException(nameof(question), "Trivia sub-stage must have a question.");

            var trivia = new TriviaSubStage
            {
                Id = id,
                Title = title,
                Order = order,
                Points = points,
                Question = question
            };

            if (clues != null)
            {
                foreach (var clue in clues)
                {
                    trivia.AddClue(clue);
                }
            }

            return trivia;
        }
    }
}
