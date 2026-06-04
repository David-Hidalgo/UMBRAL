using MediatR;
using MissionService.Application.DTOs;

namespace MissionService.Application.Queries;

public sealed record GetMissionByIdQuery(Guid Id) : IRequest<MissionDetailDto>;
