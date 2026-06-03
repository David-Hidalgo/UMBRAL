using System;
using System.Collections.Generic;

namespace MissionService.Domain.Entities
{
    public class TriviaSubStage : SubStage
    {
        private readonly List<Question> _questions = new();
        public IReadOnlyCollection<Question> Questions => _questions.AsReadOnly();

        private TriviaSubStage() : base() { }

        public static TriviaSubStage Create(Guid id, string title, int order, List<Question> questions)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Substage ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Substage title cannot be empty.", nameof(title));
            if (questions == null || questions.Count == 0)
                throw new ArgumentException("Trivia sub-stage must contain at least one question.", nameof(questions));

            var trivia = new TriviaSubStage
            {
                Id = id,
                Title = title,
                Order = order
            };

            foreach (var q in questions)
            {
                trivia.AddQuestion(q);
            }

            return trivia;
        }

        private void AddQuestion(Question question)
        {
            if (question == null)
                throw new ArgumentNullException(nameof(question));
            _questions.Add(question);
        }
    }
}
