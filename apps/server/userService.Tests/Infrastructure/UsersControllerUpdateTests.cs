using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Application.Commands;
using UserService.Application.DTOs;
using UserService.Presentation.Controllers;
using Xunit;

namespace UserService.Tests.Infrastructure;

public class UsersControllerUpdateTests
{
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);

    private UsersController BuildController(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        return new UsersController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private void SetupMediatorSuccess()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);
    }

    // ─── Reflection ───────────────────────────────────────────────────────────

    [Fact]
    public void UpdateMethod_HasHttpPutWithIdRoute()
    {
        var attr = typeof(UsersController).GetMethod(nameof(UsersController.Update))!
            .GetCustomAttribute<HttpPutAttribute>();

        attr.Should().NotBeNull();
        attr!.Template.Should().Be("{id:guid}");
    }

    [Fact]
    public void UpdateMethod_Produces204And403And404()
    {
        var attrs = typeof(UsersController).GetMethod(nameof(UsersController.Update))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>().ToList();

        attrs.Should().Contain(a => a.StatusCode == StatusCodes.Status204NoContent);
        attrs.Should().Contain(a => a.StatusCode == StatusCodes.Status403Forbidden);
        attrs.Should().Contain(a => a.StatusCode == StatusCodes.Status404NotFound);
    }

    // ─── Admin ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_AsAdmin_CanEditAnyUserAndChangeRole()
    {
        SetupMediatorSuccess();
        var targetId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "admin"));

        var result = await controller.Update(targetId, new UpdateUserRequest("New Name", "operator"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // ─── Usuario propietario ────────────────────────────────────────────────────

    [Fact]
    public async Task Update_AsPlayer_EditingOwnProfileWithSameRole_ReturnsNoContent()
    {
        SetupMediatorSuccess();
        var ownId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(ownId, new UpdateUserRequest("New Name", "player"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_AsOperator_EditingOwnProfileWithSameRole_ReturnsNoContent()
    {
        SetupMediatorSuccess();
        var ownId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "operator"));

        var result = await controller.Update(ownId, new UpdateUserRequest("New Name", "operator"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    // ─── Forbidden ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Update_AsPlayer_EditingAnotherUser_ReturnsForbid()
    {
        var ownId = Guid.NewGuid();
        var otherId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(otherId, new UpdateUserRequest("New Name", "player"), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_AsPlayer_AttemptingToChangeOwnRole_ReturnsForbid()
    {
        var ownId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(ownId, new UpdateUserRequest("New Name", "admin"), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_AsNonAdminWithoutSubClaim_ReturnsForbid()
    {
        var controller = BuildController(new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(Guid.NewGuid(), new UpdateUserRequest("New Name", "player"), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Update_AsOperator_AttemptingToChangeOwnRole_ReturnsForbid()
    {
        var ownId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "operator"));

        var result = await controller.Update(ownId, new UpdateUserRequest("New Name", "admin"), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
