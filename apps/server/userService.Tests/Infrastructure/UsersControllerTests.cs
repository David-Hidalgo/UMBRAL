using System;
using System.Collections.Generic;
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
using UserService.Application.Queries;
using UserService.Presentation.Controllers;
using Xunit;
using Unit = MediatR.Unit;

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

    [Fact]
    public void GetMethod_HasHttpGetAttribute()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Get))!
            .GetCustomAttribute<HttpGetAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void GetMethod_Produces200OK()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Get))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(a => a.StatusCode == StatusCodes.Status200OK);
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WithValidRequest_Returns201CreatedResult()
    {
        // Arrange
        var newId = Guid.NewGuid();
        var request = new CreateUserRequest("test@umbral.io", "Test User");

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
        var request = new CreateUserRequest("test@umbral.io", "Test User");

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
        var request = new CreateUserRequest("test@umbral.io", "Test User");

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
        var request = new CreateUserRequest("test@umbral.io", "Test User");
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
        captured.Role.Should().Be("operator");
    }

    [Fact]
    public async Task Create_CallsMediatorExactlyOnce()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User");

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

    [Fact]
    public async Task Get_WithValidParameters_Returns200OkResult()
    {
        // Arrange
        var search = "test";
        var role = "admin";
        var expectedUsers = new List<UserDto>
        {
            new UserDto(Guid.NewGuid(), "keycloak-1", "user1@umbral.io", "User One", "admin", DateTime.UtcNow, IsEnabled: true)
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = await _sut.Get(search, role, CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Get_WithValidParameters_ReturnsExpectedUsers()
    {
        // Arrange
        var search = "test";
        var role = "admin";
        var expectedUsers = new List<UserDto>
        {
            new UserDto(Guid.NewGuid(), "keycloak-1", "user1@umbral.io", "User One", "admin", DateTime.UtcNow, IsEnabled: true)
        };

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedUsers);

        // Act
        var result = (OkObjectResult)await _sut.Get(search, role, CancellationToken.None);

        // Assert
        result.Value.Should().BeEquivalentTo(expectedUsers);
    }

    [Fact]
    public async Task Get_SendsQueryWithCorrectData()
    {
        // Arrange
        var search = "test";
        var role = "admin";
        GetUsersQuery? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<UserDto>>, CancellationToken>((query, _) => captured = (GetUsersQuery)query)
            .ReturnsAsync(Array.Empty<UserDto>());

        // Act
        await _sut.Get(search, role, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.SearchTerm.Should().Be(search);
        captured.Role.Should().Be(role);
    }

    [Fact]
    public async Task Get_CallsMediatorExactlyOnce()
    {
        // Arrange
        var search = "test";
        var role = "admin";

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<UserDto>());

        // Act
        await _sut.Get(search, role, CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Get_WithNullParameters_PassesNullToQuery()
    {
        // Arrange
        GetUsersQuery? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<UserDto>>, CancellationToken>((query, _) => captured = (GetUsersQuery)query)
            .ReturnsAsync(Array.Empty<UserDto>());

        // Act
        await _sut.Get(null, null, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.SearchTerm.Should().BeNull();
        captured.Role.Should().BeNull();
    }

    // ─── Sad paths ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_WhenMediatorThrows_PropagatesException()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User");

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
    public async Task Create_WithExplicitRole_PassesRoleToCommand(string role)
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User", role);
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
    public async Task Create_WithNullRole_DefaultsToOperator()
    {
        // Arrange
        var request = new CreateUserRequest("test@umbral.io", "Test User");
        CreateUserCommand? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Guid>, CancellationToken>((cmd, _) => captured = (CreateUserCommand)cmd)
            .ReturnsAsync(Guid.NewGuid());

        // Act
        await _sut.Create(request, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.Role.Should().Be("operator");
    }

    [Fact]
    public async Task Get_WhenMediatorThrows_PropagatesException()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Upstream failure"));

        // Act
        var act = async () => await _sut.Get("search", "role", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Upstream*");
    }

    // ─── DELETE /api/users/{keycloakId} ──────────────────────────────────────

    [Fact]
    public void DeleteMethod_HasHttpDeleteAttribute()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Delete))!
            .GetCustomAttribute<HttpDeleteAttribute>()
            .Should().NotBeNull();
    }

    [Fact]
    public void DeleteMethod_Produces204NoContent()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Delete))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(a => a.StatusCode == StatusCodes.Status204NoContent);
    }

    [Fact]
    public void DeleteMethod_Produces404NotFound()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Delete))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(a => a.StatusCode == StatusCodes.Status404NotFound);
    }

    [Fact]
    public void DeleteMethod_Produces409Conflict()
    {
        typeof(UsersController)
            .GetMethod(nameof(UsersController.Delete))!
            .GetCustomAttributes<ProducesResponseTypeAttribute>()
            .Should().Contain(a => a.StatusCode == StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Delete_WithValidId_Returns204NoContent()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        var result = await _sut.Delete("kc-user-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_WhenMediatorThrowsKeyNotFoundException_Returns404NotFound()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new KeyNotFoundException("User not found."));

        // Act
        var result = await _sut.Delete("kc-user-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Delete_WhenMediatorThrowsInvalidOperationException_Returns409Conflict()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("User is already inactive."));

        // Act
        var result = await _sut.Delete("kc-user-123", CancellationToken.None);

        // Assert
        result.Should().BeOfType<ConflictResult>();
    }

    [Fact]
    public async Task Delete_SendsCommandWithCorrectKeycloakId()
    {
        // Arrange
        const string keycloakId = "kc-user-456";
        DeleteUserCommand? captured = null;

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<Unit>, CancellationToken>((cmd, _) => captured = (DeleteUserCommand)cmd)
            .ReturnsAsync(Unit.Value);

        // Act
        await _sut.Delete(keycloakId, CancellationToken.None);

        // Assert
        captured.Should().NotBeNull();
        captured!.KeycloakUserId.Should().Be(keycloakId);
    }

    [Fact]
    public async Task Delete_CallsMediatorExactlyOnce()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        // Act
        await _sut.Delete("kc-user-123", CancellationToken.None);

        // Assert
        _mediatorMock.Verify(
            m => m.Send(It.IsAny<DeleteUserCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
