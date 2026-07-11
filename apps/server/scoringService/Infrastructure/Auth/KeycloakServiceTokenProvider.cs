using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ScoringService.Application.Abstractions;

namespace ScoringService.Infrastructure.Auth
{
    /// <summary>
    /// Obtiene y cachea un token client_credentials de Keycloak para la identidad de servicio
    /// del scoring. Refresca el token unos segundos antes de que expire.
    /// </summary>
    public sealed class KeycloakServiceTokenProvider : IServiceTokenProvider
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly KeycloakServiceOptions _options;
        private readonly SemaphoreSlim _mutex = new(1, 1);

        private string? _cachedToken;
        private DateTime _expiresAtUtc;

        public KeycloakServiceTokenProvider(HttpClient httpClient, IOptions<KeycloakServiceOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (IsCachedTokenValid())
                return _cachedToken!;

            await _mutex.WaitAsync(cancellationToken);
            try
            {
                if (IsCachedTokenValid())
                    return _cachedToken!;

                var tokenEndpoint =
                    $"{_options.BaseUrl.TrimEnd('/')}/realms/{_options.Realm}/protocol/openid-connect/token";

                using var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
                {
                    Content = new FormUrlEncodedContent(new Dictionary<string, string>
                    {
                        ["grant_type"] = "client_credentials",
                        ["client_id"] = _options.ClientId,
                        ["client_secret"] = _options.ClientSecret,
                    }),
                };

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var token = JsonSerializer.Deserialize<TokenResponse>(body, SerializerOptions)
                    ?? throw new InvalidOperationException("Respuesta de token de Keycloak vacía.");

                _cachedToken = token.AccessToken;
                // Refresca 30s antes de la expiración real para evitar carreras.
                var refreshInSeconds = Math.Max(0, token.ExpiresIn - 30);
                _expiresAtUtc = DateTime.UtcNow.AddSeconds(refreshInSeconds);
                return _cachedToken;
            }
            finally
            {
                _mutex.Release();
            }
        }

        private bool IsCachedTokenValid()
            => _cachedToken is not null && DateTime.UtcNow < _expiresAtUtc;

        private sealed record TokenResponse(
            [property: JsonPropertyName("access_token")] string AccessToken,
            [property: JsonPropertyName("expires_in")] int ExpiresIn);
    }
}
