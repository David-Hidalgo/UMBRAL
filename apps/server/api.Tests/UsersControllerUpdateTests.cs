using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using api.Application.Commands;
using api.Application.DTOs;
using api.Application.Interfaces;
using api.Infrastructure.Controllers;
using Xunit;

namespace api.Tests;

public class UsersControllerUpdateTests
{
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly Mock<IUserServiceClient> _clientMock = new(MockBehavior.Strict);

    private UsersController BuildController(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth", ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        return new UsersController(_mediatorMock.Object, _clientMock.Object)
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

    [Fact]
    public async Task Update_AsAdmin_ReturnsNoContent()
    {
        SetupMediatorSuccess();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "admin"));

        var result = await controller.Update(Guid.NewGuid(), new UpdateUserRequest("Name", "admin"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_AsPlayer_OwnProfileSameRole_ReturnsNoContent()
    {
        SetupMediatorSuccess();
        var ownId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(ownId, new UpdateUserRequest("Name", "player"), CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_AsPlayer_AnotherUser_ReturnsForbid()
    {
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(Guid.NewGuid(), new UpdateUserRequest("Name", "player"), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_AsPlayer_ChangingOwnRole_ReturnsForbid()
    {
        var ownId = Guid.NewGuid();
        var controller = BuildController(
            new Claim(ClaimTypes.NameIdentifier, ownId.ToString()),
            new Claim(ClaimTypes.Role, "player"));

        var result = await controller.Update(ownId, new UpdateUserRequest("Name", "operator"), CancellationToken.None);

        result.Should().BeOfType<ForbidResult>();
        _mediatorMock.Verify(m => m.Send(It.IsAny<UpdateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
