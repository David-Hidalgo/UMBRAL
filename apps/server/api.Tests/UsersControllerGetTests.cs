using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using api.Application.DTOs;
using api.Application.Interfaces;
using api.Infrastructure.Controllers;
using Xunit;

namespace api.Tests;

public class UsersControllerGetTests
{
    private readonly Mock<IMediator> _mediatorMock = new(MockBehavior.Strict);
    private readonly Mock<IUserServiceClient> _clientMock = new(MockBehavior.Strict);

    private UsersController BuildController()
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Role, "admin") }, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new UsersController(_mediatorMock.Object, _clientMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            }
        };
    }

    private static UserDto SampleUser() =>
        new(Guid.NewGuid(), Guid.NewGuid().ToString(), "user@umbral.io", "User Name", "player", DateTime.UtcNow);

    [Fact]
    public async Task Get_ReturnsOkWithUserList()
    {
        // Arrange
        var users = new List<UserDto> { SampleUser(), SampleUser() };
        _clientMock
            .Setup(c => c.GetUsersAsync(null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(users);
        var controller = BuildController();

        // Act
        var result = await controller.Get(null, null, CancellationToken.None);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeEquivalentTo(users);
    }

    [Fact]
    public async Task Get_PassesSearchAndRoleFiltersToClient()
    {
        // Arrange
        _clientMock
            .Setup(c => c.GetUsersAsync("john", "admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserDto>());
        var controller = BuildController();

        // Act
        var result = await controller.Get("john", "admin", CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        _clientMock.Verify(
            c => c.GetUsersAsync("john", "admin", It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
