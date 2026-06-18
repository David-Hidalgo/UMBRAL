using System;

namespace MissionService.Domain.Entities
{
    public class Answer
    {
        public Guid Id { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public bool IsCorrect { get; private set; }
        public Guid QuestionId { get; private set; }

        private Answer() { }

        public static Answer Create(Guid id, string text, bool isCorrect)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Answer ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Answer text cannot be empty.", nameof(text));

            return new Answer
            {
                Id = id,
                Text = text,
                IsCorrect = isCorrect
            };
        }

        public void Update(string text, bool isCorrect)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Answer text cannot be empty.", nameof(text));
            Text = text;
            IsCorrect = isCorrect;
        }
    }
}
