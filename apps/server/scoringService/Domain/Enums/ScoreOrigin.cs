namespace ScoringService.Domain.Enums
{
    /// <summary>Origen de un asiento de puntaje: garantiza la trazabilidad exigida por RB-07.</summary>
    public enum ScoreOrigin
    {
        EvidenceValidated = 0,
        Penalty = 1,
        ManualAdjustment = 2
    }
}
