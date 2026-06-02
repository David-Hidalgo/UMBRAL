using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using api.Application.Commands;
using api.Application.DTOs;
using api.Application.Interfaces;

namespace api.Infrastructure.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IUserServiceClient _userService;

    public UsersController(IMediator mediator, IUserServiceClient userService)
    {
        _mediator = mediator;
        _userService = userService;
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var userId = await _mediator.Send(new CreateUserCommand(request), cancellationToken);
        return Created($"/api/users/{userId}", new { id = userId });
    }

    [HttpGet]
    [Authorize(Roles = "admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] string? role, CancellationToken cancellationToken)
    {
        var users = await _userService.GetUsersAsync(search, role, cancellationToken);
        return Ok(users);
    }
}
