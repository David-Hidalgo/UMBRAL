using System;
using MediatR;
using api.Application.DTOs;

namespace api.Application.Commands;

public sealed record CreateUserCommand(CreateUserRequest Request) : IRequest<Guid>;
