using System;
using System.Collections.Generic;
using System.Net.Http;
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

    public async Task UpdateUserAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("UserService:BaseUrl is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Put, $"{_options.BaseUrl.TrimEnd('/')}/api/users/{id}")
        {
            Content = JsonContent.Create(request)
        };

        var rawHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(rawHeader) && AuthenticationHeaderValue.TryParse(rawHeader, out var parsedHeader))
            message.Headers.Authorization = parsedHeader;

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new UserServiceException($"UserService returned {(int)response.StatusCode}: {body}");
        }
    }

    public async Task<IEnumerable<UserDto>> GetUsersAsync(string? search = null, string? role = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("UserService:BaseUrl is not configured.");

        var uriBuilder = new UriBuilder($"{_options.BaseUrl.TrimEnd('/')}/api/users");
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        if (!string.IsNullOrWhiteSpace(role))
            queryParams.Add($"role={Uri.EscapeDataString(role)}");
        
        if (queryParams.Count > 0)
            uriBuilder.Query = string.Join("&", queryParams);

        using var message = new HttpRequestMessage(HttpMethod.Get, uriBuilder.ToString());

        var rawHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(rawHeader) && AuthenticationHeaderValue.TryParse(rawHeader, out var parsedHeader))
            message.Headers.Authorization = parsedHeader;

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new UserServiceException($"UserService returned {(int)response.StatusCode}: {body}");

        if (string.IsNullOrWhiteSpace(body))
            return Array.Empty<UserDto>();

        try
        {
            var users = JsonSerializer.Deserialize<IEnumerable<UserDto>>(body, SerializerOptions);
            return users ?? Array.Empty<UserDto>();
        }
        catch (JsonException ex)
        {
            throw new UserServiceException($"UserService returned a non-JSON response: {body}", ex);
        }
    }

    private sealed record UserCreatedResponse(Guid Id);
}
