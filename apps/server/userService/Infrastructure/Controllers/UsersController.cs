using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Commands;
using UserService.Application.DTOs;
using UserService.Application.Queries;

namespace UserService.Infrastructure.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public sealed class UsersController : ControllerBase
    {
        private readonly IMediator _mediator;

        public UsersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
        {
            var userId = await _mediator.Send(new CreateUserCommand(request.Email, request.Name, request.Password, request.Role ?? "player"), cancellationToken);
            return Created($"/api/users/{userId}", new { id = userId });
        }

        [HttpGet]
        [Authorize(Roles = "admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get([FromQuery] string? search, [FromQuery] string? role, CancellationToken cancellationToken)
        {
            var users = await _mediator.Send(new GetUsersQuery(search, role), cancellationToken);
            return Ok(users);
        }

        [HttpDelete("{keycloakId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Delete(string keycloakId, CancellationToken cancellationToken)
        {
            try
            {
                await _mediator.Send(new DeleteUserCommand(keycloakId), cancellationToken);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException)
            {
                return Conflict();
            }
        }

        [HttpPut("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            if (!User.IsInRole("admin"))
            {
                var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                if (string.IsNullOrEmpty(callerId) || !string.Equals(callerId, id.ToString(), StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                var currentRole = User.IsInRole("operator") ? "operator" : "player";
                if (!string.Equals(request.Role, currentRole, StringComparison.OrdinalIgnoreCase))
                    return Forbid();
            }

            await _mediator.Send(new UpdateUserCommand(id, request.Name, request.Role), cancellationToken);
            return NoContent();
        }
    }
}
