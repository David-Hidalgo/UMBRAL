using System;
using System.Security.Claims;
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

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (!User.IsInRole("admin"))
        {
            var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

            // Un usuario no-admin solo puede editar su propio perfil.
            if (string.IsNullOrEmpty(callerId) || !string.Equals(callerId, id.ToString(), StringComparison.OrdinalIgnoreCase))
                return Forbid();

            // Un usuario no-admin no puede cambiar su propio rol (integridad del rol).
            var currentRole = User.IsInRole("operator") ? "operator" : "player";
            if (!string.Equals(request.Role, currentRole, StringComparison.OrdinalIgnoreCase))
                return Forbid();
        }

        try
        {
            await _mediator.Send(new UpdateUserCommand(id, request), cancellationToken);
            return NoContent();
        }
        catch (api.Application.Exceptions.UserServiceException ex)
        {
            const string prefix = "UserService returned ";
            if (ex.Message.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                ex.Message.Length >= prefix.Length + 3 &&
                int.TryParse(ex.Message.Substring(prefix.Length, 3), out var status))
            {
                var idx = ex.Message.IndexOf(": ", StringComparison.Ordinal);
                var body = idx >= 0 ? ex.Message[(idx + 2)..] : ex.Message;
                var result = Content(body, "application/json");
                result.StatusCode = status;
                return result;
            }

            throw;
        }
    }
}
