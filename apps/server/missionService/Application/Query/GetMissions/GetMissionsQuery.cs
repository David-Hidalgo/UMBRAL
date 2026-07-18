using System.Collections.Generic;
using MediatR;
using MissionService.Application.DTOs;

namespace MissionService.Application.Queries
{
    /// <summary>
    /// Consulta CQRS para recuperar una lista filtrada de misiones.
    /// Ambos parámetros son opcionales; cuando se omiten, se devuelven todas las misiones.
    /// Validado mediante MediatR ValidationBehavior antes de que se ejecute el handler.
    /// </summary>
    public sealed record GetMissionsQuery(
        string? SearchTerm,
        string? Status
    ) : IRequest<IEnumerable<MissionSummaryDto>>;
}
