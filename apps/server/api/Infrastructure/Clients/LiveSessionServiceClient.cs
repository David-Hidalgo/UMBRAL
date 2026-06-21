using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using api.Application.DTOs;
using api.Application.Exceptions;
using api.Application.Interfaces;

namespace api.Infrastructure.Clients
{
    public sealed class LiveSessionServiceClient : ILiveSessionServiceClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly LiveSessionServiceOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LiveSessionServiceClient(
            HttpClient httpClient,
            IOptions<LiveSessionServiceOptions> options,
            IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _options = options.Value;
            _httpContextAccessor = httpContextAccessor;
        }

        private void PropagateAuthorizationHeader(HttpRequestMessage message)
        {
            var rawHeader = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(rawHeader) && AuthenticationHeaderValue.TryParse(rawHeader, out var parsedHeader))
            {
                message.Headers.Authorization = parsedHeader;
            }
        }

        private string GetRequiredBaseUrl()
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl))
            {
                throw new InvalidOperationException("LiveSessionService:BaseUrl is not configured.");
            }
            return _options.BaseUrl.TrimEnd('/');
        }

        public async Task<Guid> CreateSessionAsync(CreateLiveSessionRequest request, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/sessions")
            {
                Content = JsonContent.Create(request)
            };
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                throw new MissionServiceException("LiveSessionService returned an empty response.");
            }

            try
            {
                return JsonSerializer.Deserialize<Guid>(body, SerializerOptions);
            }
            catch (JsonException ex)
            {
                throw new MissionServiceException($"LiveSessionService returned a non-JSON response: {body}", ex);
            }
        }

        public async Task<IEnumerable<LiveSessionSummaryDto>> GetAllSessionsAsync(Guid? missionId, string? status, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            var queryString = HttpUtility.ParseQueryString(string.Empty);
            if (missionId.HasValue)
                queryString["missionId"] = missionId.Value.ToString();
            if (!string.IsNullOrWhiteSpace(status))
                queryString["status"] = status;

            var qs = queryString.Count > 0 ? $"?{queryString}" : string.Empty;
            using var message = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/sessions{qs}");
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }

            try
            {
                return JsonSerializer.Deserialize<IEnumerable<LiveSessionSummaryDto>>(body, SerializerOptions) ?? [];
            }
            catch (JsonException ex)
            {
                throw new MissionServiceException($"LiveSessionService returned a non-JSON response: {body}", ex);
            }
        }

        public async Task<LiveSessionDetailDto> GetSessionByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            using var message = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/api/sessions/{id}");
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }

            try
            {
                return JsonSerializer.Deserialize<LiveSessionDetailDto>(body, SerializerOptions)
                    ?? throw new MissionServiceException("LiveSessionService returned an empty session detail.");
            }
            catch (JsonException ex)
            {
                throw new MissionServiceException($"LiveSessionService returned a non-JSON response: {body}", ex);
            }
        }

        public async Task TransitionStatusAsync(Guid id, TransitionStatusRequest request, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            using var message = new HttpRequestMessage(HttpMethod.Patch, $"{baseUrl}/api/sessions/{id}/status")
            {
                Content = JsonContent.Create(request)
            };
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }
        }

        public async Task<Guid> CreateTeamAsync(Guid id, CreateTeamRequest request, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/sessions/{id}/teams")
            {
                Content = JsonContent.Create(request)
            };
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                throw new MissionServiceException("LiveSessionService returned an empty response.");
            }

            try
            {
                return JsonSerializer.Deserialize<Guid>(body, SerializerOptions);
            }
            catch (JsonException ex)
            {
                throw new MissionServiceException($"LiveSessionService returned a non-JSON response: {body}", ex);
            }
        }

        public async Task JoinTeamAsync(Guid id, Guid teamId, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/sessions/{id}/teams/{teamId}/join");
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }
        }

        public async Task AdvanceStageAsync(Guid id, AdvanceStageRequest request, CancellationToken cancellationToken = default)
        {
            var baseUrl = GetRequiredBaseUrl();
            using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/api/sessions/{id}/advance")
            {
                Content = JsonContent.Create(request)
            };
            PropagateAuthorizationHeader(message);

            using var response = await _httpClient.SendAsync(message, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new MissionServiceException((int)response.StatusCode, body, $"LiveSessionService returned {(int)response.StatusCode}: {body}");
            }
        }
    }
}
