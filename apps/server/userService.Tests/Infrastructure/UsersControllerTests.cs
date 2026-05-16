using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Application.Commands;
using UserService.Application.DTOs;
using UserService.Infrastructure.Controllers;
using Xunit;

namespace UserService.Tests.Infrastructure;

public class UsersControllerTests
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly UsersController _sut;

    public UsersControllerTests()
    {
        _mediatorMock = new Mock<IMediator>(MockBehavior.Strict);
        _sut = new UsersController(_mediatorMock.Object);
    }

    // ─── Attribute / routing reflection ──────────────────────────────────────

    [Fact]
    public void UsersController_HasApiControllerAttribute()
    {
        typeof(UsersController)
            .GetCustomAttribute<ApiControllerAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void UsersController_RouteIsApiUsers()
    {
        typeof(UsersController)
            .GetCustomAttribute<RouteAttribute>()!
            .Template.Should().Be("api/users");
    }

    [Fact]
    public void CreateMethod_HasHttpPostAttribute()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Create))!
            .GetCustomAttribute<HttpPostAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void CreateMethod_Produces201Created()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Create))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(a => a.StatusCode == StatusCodes.Status201Created);
    }

    [Fact]
    public void CreateMethod_Produces400BadRequest()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Create))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(a => a.StatusCode == StatusCodes.Status400BadRequest);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201CreatedResult()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var result = await _sut.Create(request, CancellationToken.None);

        // Assert
        result.Should().BeOfType<CreatedResult>();
    }

    [Fact]
    public async Task Create_WithValidRequest_ResponseBodyContainsUserId()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var result = (CreatedResult)await _sut.Create(request, CancellationToken.None);

        // Assert
        result.Value.Should().BeEquivalentTo(new { id = newId });
    }

    [Fact]
    public async Task Create_WithValidRequest_LocationHeaderContainsUserId()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(newId);

        // Act
        var result = (CreatedResult)await _sut.Create(request, CancellationToken.None);

        // Assert
        result.Location.Should().Contain(newId.ToString());
    }

    [Fact]
    public async Task Create_SendsCommandWithCorrectData()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");
        CreateUserCommand? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((cmd, _) => captured = (CreateUserCommand)cmd)
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Create(request, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Email.Should().Be(request.Email);
        captured.Name.Should().Be(request.Name);
        captured.Password.Should().Be(request.Password);
        captured.Role.Should().Be("player");
    }

    [Fact]
    public async Task Create_CallsMediatorExactlyOnce()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Create(request, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ─── Sad paths ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenMediatorThrows_PropagatesException()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Upstream failure"));

        // Act
        var act = async () => await _sut.Create(request, CancellationToken.None);

        // Assert
        // El global exception handler en Program.cs convierte esto en 500.
        // El controller en sí propaga la excepción sin atraparla.
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Upstream*");
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("operator")]
    [InlineData("player")]
    public async Task Create_WithExplicitRole_PassesRoleToCommand(string role)
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!", role);
        CreateUserCommand? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((cmd, _) => captured = (CreateUserCommand)cmd)
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Create(request, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Role.Should().Be(role);
    }

    [Fact]
    public async Task Create_WithNullRole_DefaultsToPlayer()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User", "Password123!");
        CreateUserCommand? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((cmd, _) => captured = (CreateUserCommand)cmd)
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Create(request, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Role.Should().Be("player");
    }
}
