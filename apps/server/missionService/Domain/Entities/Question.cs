using System;
using System.Collections.Generic;
using System.Linq;
using MissionService.Domain.Exceptions;

namespace MissionService.Domain.Entities
{
    public class Question
    {
        public Guid Id { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public int Points { get; private set; }
        public Guid TriviaSubStageId { get; private set; }

        private readonly List<Answer> _answers = new();
        public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();

        private Question() { }

        public static Question Create(Guid id, string text, int points, List<Answer> answers)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Question ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Question text cannot be empty.", nameof(text));
            if (points < 0)
                throw new ArgumentException("Question points cannot be negative.", nameof(points));
            if (answers == null || answers.Count == 0)
                throw new ArgumentException("Question must have at least one answer.", nameof(answers));

            // Validate that at least one answer is marked correct
            if (!answers.Any(a => a.IsCorrect))
                throw new TriviaRequiresCorrectAnswerException(text);

            var question = new Question
            {
                Id = id,
                Text = text,
                Points = points
            };

            foreach (var a in answers)
            {
                question.AddAnswer(a);
            }

            return question;
        }

        private void AddAnswer(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));
            _answers.Add(answer);
        }
    }
}
