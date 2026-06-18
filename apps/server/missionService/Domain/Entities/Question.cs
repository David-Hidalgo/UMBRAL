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
        public Guid TriviaSubStageId { get; private set; }

        private readonly List<Answer> _answers = new();
        public IReadOnlyCollection<Answer> Answers => _answers.AsReadOnly();

        private Question() { }

        public static Question Create(Guid id, string text, List<Answer> answers)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Question ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Question text cannot be empty.", nameof(text));
            if (answers == null || answers.Count == 0)
                throw new ArgumentException("Question must have at least one answer.", nameof(answers));

            // Validar que al menos una respuesta esté marcada como correcta
            if (!answers.Any(a => a.IsCorrect))
                throw new TriviaRequiresCorrectAnswerException(text);

            var question = new Question
            {
                Id = id,
                Text = text
            };

            foreach (var a in answers)
            {
                question.AddAnswer(a);
            }

            return question;
        }

        public void Update(string text, List<Answer> newAnswers)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Question text cannot be empty.", nameof(text));
            if (newAnswers == null || newAnswers.Count == 0)
                throw new ArgumentException("Question must have at least one answer.", nameof(newAnswers));

            if (!newAnswers.Any(a => a.IsCorrect))
                throw new TriviaRequiresCorrectAnswerException(text);

            Text = text;

            // Eliminar las respuestas que no estén en la nueva lista
            var answersToRemove = _answers.Where(a => !newAnswers.Any(na => na.Id == a.Id)).ToList();
            foreach (var answer in answersToRemove)
            {
                _answers.Remove(answer);
            }

            // Actualizar existente o agregar nueva
            foreach (var newAnswer in newAnswers)
            {
                var existing = _answers.FirstOrDefault(a => a.Id == newAnswer.Id);
                if (existing != null)
                {
                    existing.Update(newAnswer.Text, newAnswer.IsCorrect);
                }
                else
                {
                    AddAnswer(newAnswer);
                }
            }
        }

        private void AddAnswer(Answer answer)
        {
            if (answer == null)
                throw new ArgumentNullException(nameof(answer));
            _answers.Add(answer);
        }
    }
}
