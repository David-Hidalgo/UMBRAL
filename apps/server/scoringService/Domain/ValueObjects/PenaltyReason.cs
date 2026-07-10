using ScoringService.Domain.Exceptions;

namespace ScoringService.Domain.ValueObjects
{
    /// <summary>
    /// Value Object inmutable que garantiza que toda penalización lleva un motivo no vacío (RB-06).
    /// La sola construcción es la que hace cumplir la invariante.
    /// </summary>
    public record PenaltyReason
    {
        public string Value { get; }

        public PenaltyReason(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidPenaltyReasonException();
            Value = value.Trim();
        }

        public override string ToString() => Value;
    }
}
