using System;

namespace ScoringService.Domain.Exceptions
{
    /// <summary>La evidencia referida no existe.</summary>
    public sealed class SubmissionNotFoundException : DomainException
    {
        public SubmissionNotFoundException(Guid submissionId)
            : base($"No se encontró la evidencia '{submissionId}'.") { }
    }
}
