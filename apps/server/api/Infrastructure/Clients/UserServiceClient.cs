using System;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using api.Application.DTOs;
using api.Application.Exceptions;
using api.Application.Interfaces;

namespace api.Infrastructure.Clients;

public sealed class UserServiceClient : IUserServiceClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly UserServiceOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserServiceClient(HttpClient httpClient, IOptions<UserServiceOptions> options, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("UserService:BaseUrl is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/api/users")
        {
            Content = JsonContent.Create(request)
        };

        var rawHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(rawHeader) && AuthenticationHeaderValue.TryParse(rawHeader, out var parsedHeader))
            message.Headers.Authorization = parsedHeader;

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new UserServiceException($"UserService returned {(int)response.StatusCode}: {body}");

        if (string.IsNullOrWhiteSpace(body))
            throw new UserServiceException("UserService returned an empty response.");

        try
        {
            var payload = JsonSerializer.Deserialize<UserCreatedResponse>(body, SerializerOptions);
            if (payload is not null)
                return payload.Id;
        }
        catch (JsonException ex)
        {
            throw new UserServiceException($"UserService returned a non-JSON response: {body}", ex);
        }

        throw new UserServiceException("UserService returned an unexpected response payload.");
    }

    private sealed record UserCreatedResponse(Guid Id);
}
