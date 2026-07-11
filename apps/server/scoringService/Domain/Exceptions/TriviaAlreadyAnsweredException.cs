using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>
    /// La Trivia es de un solo intento: una vez respondida (correcta o incorrectamente), el
    /// equipo no puede volver a intentarla. Pierde su oportunidad si falla.
    /// </summary>
    public sealed class TriviaAlreadyAnsweredException : DomainException
    {
        public TriviaAlreadyAnsweredException(Guid subStageId)
            : base($"Ya respondiste esta pregunta ('{subStageId}'); no puedes volver a intentarlo.") { }
    }
}
