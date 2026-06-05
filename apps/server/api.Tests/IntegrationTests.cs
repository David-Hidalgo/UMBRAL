using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using api.Application.DTOs;
using api.Application.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TestUtils.TestAuth;
using Xunit;

namespace api.Tests;

public class IntegrationTests : IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;

    public IntegrationTests()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IUserServiceClient>(new FakeUserServiceClient());
                    services.AddTestAuthentication();
                });
            });
    }

    [Fact]
    public async Task PostUsers_WithAdminRole_ReturnsCreated()
    {
        var client = _factory.CreateClient();

        var payload = new { email = "x@x.com", name = "X", password = "Passw0rd!" };

        var resp = await client.PostAsJsonAsync("/api/users", payload);

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    private sealed class FakeUserServiceClient : IUserServiceClient
    {
        public Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Guid.NewGuid());

        public Task<IEnumerable<UserDto>> GetUsersAsync(string? search = null, string? role = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IEnumerable<UserDto>>(Array.Empty<UserDto>());

        public Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
