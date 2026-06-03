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
using api.Application.Interfaces;
using api.Infrastructure.Controllers;
using Xunit;

namespace api.Tests;

public class UsersControllerDeleteTests
{
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly Mock<IUserServiceClient> _clientMock = new(MockBehavior.Strict);

    private UsersController BuildAdminController()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "admin")], "TestAuth",
            ClaimTypes.Name, ClaimTypes.Role);
        return new UsersController(_mediatorMock.Object, _clientMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    [Fact]
    public async Task Delete_WithAdminRole_ReturnsNoContent()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        var result = await BuildAdminController().Delete("keycloak-id-123", CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_DelegatesToMediatorWithCorrectKeycloakId()
    {
        const string keycloakId = "keycloak-abc-456";
        DeleteUserCommand? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((cmd, _) => captured = (DeleteUserCommand)cmd)
            .ReturnsAsync(Unit.Value);

        await BuildAdminController().Delete(keycloakId, CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.KeycloakId.Should().Be(keycloakId);
        _mediatorMock.Verify(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
