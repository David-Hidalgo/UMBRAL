using System;
using System.Collections.Generic;
using ScoringService.Domain.Enums;

namespace ScoringService.Application.DTOs
{
    /// <summary>
    /// Vista del reto actual PARA EL JUGADOR: incluye pregunta y opciones, pero NUNCA la
    /// respuesta correcta (isCorrect) ni el código secreto. Así el jugador no puede hacer trampa.
    /// </summary>
    public sealed record PlayerChallengeDto(
        Guid SessionId,
        string Status,
        IReadOnlyList<PlayerSubStageDto> SubStages);

    public sealed record PlayerSubStageDto(
        Guid Id,
        string Title,
        int Points,
        SubmissionType Type,
        PlayerQuestionDto? Question,
        string? Objective,
        // El equipo ya no puede enviar a esta subetapa: Trivia = ya la respondió (1 intento);
        // Búsqueda del tesoro = ya la resolvió.
        bool Closed = false,
        // Ubicación del tesoro (solo Búsqueda del Tesoro): guía visual en el mapa del jugador.
        double? Latitude = null,
        double? Longitude = null,
        int? SearchRadiusMeters = null);

    public sealed record PlayerQuestionDto(Guid Id, string Text, IReadOnlyList<PlayerAnswerDto> Answers);

    /// <summary>Opción de respuesta SIN la marca de correcta.</summary>
    public sealed record PlayerAnswerDto(Guid Id, string Text);
}
