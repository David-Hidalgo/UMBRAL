using System;
using LiveSessionService.Application.DTOs;
using MediatR;

namespace LiveSessionService.Application.Queries
{
    public sealed record GetLiveSessionByIdQuery(Guid Id) : IRequest<LiveSessionDetailDto?>;
}
