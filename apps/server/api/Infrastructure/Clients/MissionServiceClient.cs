using System;
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

public sealed class MissionServiceClient : IMissionServiceClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly MissionServiceOptions _options;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MissionServiceClient(HttpClient httpClient, IOptions<MissionServiceOptions> options, IHttpContextAccessor httpContextAccessor)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> CreateMissionAsync(CreateMissionRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            throw new InvalidOperationException("MissionService:BaseUrl is not configured.");

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/api/missions")
        {
            Content = JsonContent.Create(request)
        };

        var rawHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(rawHeader) && AuthenticationHeaderValue.TryParse(rawHeader, out var parsedHeader))
            message.Headers.Authorization = parsedHeader;

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new MissionServiceException($"MissionService returned {(int)response.StatusCode}: {body}");

        if (string.IsNullOrWhiteSpace(body))
            throw new MissionServiceException("MissionService returned an empty response.");

        try
        {
            var payload = JsonSerializer.Deserialize<MissionCreatedResponse>(body, SerializerOptions);
            if (payload is not null)
                return payload.Id;
        }
        catch (JsonException ex)
        {
            throw new MissionServiceException($"MissionService returned a non-JSON response: {body}", ex);
        }

        throw new MissionServiceException("MissionService returned an unexpected response payload.");
    }

    private sealed record MissionCreatedResponse(Guid Id);
}
