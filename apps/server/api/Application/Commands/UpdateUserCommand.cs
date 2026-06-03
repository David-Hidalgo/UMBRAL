using System;
using MediatR;
using api.Application.DTOs;

namespace api.Application.Commands;

public sealed record UpdateUserCommand(Guid Id, UpdateUserRequest Request) : IRequest<Unit>;
