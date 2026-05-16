using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using UserService.Application.Interfaces;
using shared.Authentication;

namespace UserService.Infrastructure.Authentication
{
    public sealed class KeycloakUserProvisioningService : IIdentityProviderService
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly HttpClient _httpClient;
        private readonly KeycloakAdminOptions _options;

        public KeycloakUserProvisioningService(HttpClient httpClient, IOptions<KeycloakAdminOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> CreateUserAsync(string email, string name, string password, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var requestUri = $"{NormalizeBaseUrl(_options.BaseUrl)}/admin/realms/{_options.Realm}/users";
            var payload = new
            {
                username = email,
                email,
                firstName = name,
                enabled = true,
                emailVerified = false,
                credentials = new[]
                {
                    new
                    {
                        type = "password",
                        value = password,
                        temporary = false
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = JsonContent.Create(payload, options: SerializerOptions)
            };

            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode != HttpStatusCode.Created)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Keycloak user creation failed with status {(int)response.StatusCode}: {body}");
            }

            if (response.Headers.Location is null)
            {
                throw new InvalidOperationException("Keycloak did not return the created user location.");
            }

            var keycloakUserId = response.Headers.Location.Segments.Last().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(keycloakUserId))
            {
                throw new InvalidOperationException("Keycloak returned an empty user identifier.");
            }

            return keycloakUserId;
        }

        private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
        {
            var tokenRequestUri = $"{NormalizeBaseUrl(_options.BaseUrl)}/realms/{_options.Realm}/protocol/openid-connect/token";
            using var request = new HttpRequestMessage(HttpMethod.Post, tokenRequestUri)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _options.AdminClientId,
                    ["client_secret"] = _options.AdminClientSecret
                })
            };

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Keycloak admin token request failed with status {(int)response.StatusCode}: {body}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>(SerializerOptions, cancellationToken);
            if (tokenResponse is null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Keycloak did not return an access token.");
            }

            return tokenResponse.AccessToken;
        }

        private void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(_options.BaseUrl) || string.IsNullOrWhiteSpace(_options.Realm) || string.IsNullOrWhiteSpace(_options.AdminClientId) || string.IsNullOrWhiteSpace(_options.AdminClientSecret))
            {
                throw new InvalidOperationException("Keycloak settings are incomplete. Configure BaseUrl, Realm, AdminClientId and AdminClientSecret.");
            }
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            return baseUrl.TrimEnd('/');
        }

        private sealed class KeycloakTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
        }
    }
}