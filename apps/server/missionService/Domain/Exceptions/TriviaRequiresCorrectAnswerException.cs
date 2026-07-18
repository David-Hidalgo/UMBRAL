namespace MissionService.Domain.Exceptions
{
    public sealed class TriviaRequiresCorrectAnswerException : DomainException
    {
        public TriviaRequiresCorrectAnswerException(string questionText)
            : base($"Trivia question '{questionText}' must have at least one correct answer marked.") { }
    }
}
