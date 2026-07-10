namespace ScoringService.Domain.Exceptions
{
    /// <summary>RB-06: toda penalización debe registrar obligatoriamente el motivo.</summary>
    public sealed class InvalidPenaltyReasonException : DomainException
    {
        public InvalidPenaltyReasonException()
            : base("El motivo de la penalización es obligatorio.") { }
    }
}
