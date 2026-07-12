namespace ScoringService.Domain.Exceptions
{
    /// <summary>Se lanza cuando una evidencia no cumple las invariantes de creación (RB-05).</summary>
    public sealed class InvalidSubmissionException : DomainException
    {
        public InvalidSubmissionException(string message) : base(message) { }
    }
}
