using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>Una evidencia solo puede validarse una vez; reintentos deben rechazarse.</summary>
    public sealed class SubmissionAlreadyValidatedException : DomainException
    {
        public SubmissionAlreadyValidatedException(Guid submissionId)
            : base($"La evidencia '{submissionId}' ya fue validada y no puede modificarse.") { }
    }
}
