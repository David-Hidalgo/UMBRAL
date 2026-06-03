using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using api.Application.Commands;
using api.Application.DTOs;
using api.Application.Handlers;
using api.Application.Interfaces;
using Xunit;

namespace api.Tests.Application;

public class UpdateUserHandlerTests
{
    private readonly Mock<IUserServiceClient> _clientMock;
    private readonly UpdateUserHandler _sut;
    private readonly Guid _id = Guid.NewGuid();

    public UpdateUserHandlerTests()
    {
        _clientMock = new Mock<IUserServiceClient>(MockBehavior.Strict);
        var logger = new Mock<ILogger<UpdateUserHandler>>(MockBehavior.Loose);
        _sut = new UpdateUserHandler(_clientMock.Object, logger.Object);
    }

    private UpdateUserCommand Command()
        => new(_id, new UpdateUserRequest("New Name", "operator"));

    [Fact]
    public async Task Handle_DelegatesToClientWithIdAndRequest()
    {
        Guid capturedId = Guid.Empty;
        UpdateUserRequest? capturedReq = null;

        _clientMock
            .Setup(c => c.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, UpdateUserRequest, CancellationToken>((id, req, _) => { capturedId = id; capturedReq = req; })
            .Returns(Task.CompletedTask);

        await _sut.Handle(Command(), CancellationToken.None);

        capturedId.Should().Be(_id);
        capturedReq.Should().NotBeNull();
        capturedReq!.Name.Should().Be("New Name");
        capturedReq.Role.Should().Be("operator");
        _clientMock.Verify(c => c.UpdateUserAsync(_id, It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenClientThrows_PropagatesException()
    {
        _clientMock
            .Setup(c => c.UpdateUserAsync(It.IsAny<Guid>(), It.IsAny<UpdateUserRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("UserService unavailable"));

        var act = async () => await _sut.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*unavailable*");
    }
}
