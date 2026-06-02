using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using UserService.Application.DTOs;
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

        public async Task<string> CreateUserAsync(string email, string name, string password, string role, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var requestUri = $"{NormalizeBaseUrl(_options.EffectiveInternalBaseUrl)}/admin/realms/{_options.Realm}/users";
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

            await AssignRealmRoleAsync(accessToken, keycloakUserId, role, cancellationToken);

            return keycloakUserId;
        }

        private async Task AssignRealmRoleAsync(string accessToken, string userId, string roleName, CancellationToken cancellationToken)
        {
            var baseUrl = NormalizeBaseUrl(_options.EffectiveInternalBaseUrl);

            // 1. Get available realm roles for user (to bypass direct role query permissions)
            var getRolesUri = $"{baseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm/available";
            using var getRolesRequest = new HttpRequestMessage(HttpMethod.Get, getRolesUri);
            getRolesRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var getRolesResponse = await _httpClient.SendAsync(getRolesRequest, cancellationToken);
            if (!getRolesResponse.IsSuccessStatusCode)
            {
                var body = await getRolesResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to retrieve available roles for user '{userId}': {(int)getRolesResponse.StatusCode}: {body}");
            }

            var rolesJson = await getRolesResponse.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(rolesJson);
            var rolesArray = document.RootElement;

            if (rolesArray.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"Unexpected response from Keycloak available roles. Expected array, got: {rolesJson}");
            }

            JsonElement targetRole = default;
            bool found = false;
            foreach (var roleElement in rolesArray.EnumerateArray())
            {
                if (roleElement.TryGetProperty("name", out var nameProp) &&
                    string.Equals(nameProp.GetString(), roleName, StringComparison.OrdinalIgnoreCase))
                {
                    targetRole = roleElement;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                throw new InvalidOperationException($"Keycloak role '{roleName}' is not available for user '{userId}'. Available roles: {rolesJson}");
            }

            var roleJson = targetRole.GetRawText();

            // 2. Assign realm role to user
            var assignUri = $"{baseUrl}/admin/realms/{_options.Realm}/users/{userId}/role-mappings/realm";
            using var assignRequest = new HttpRequestMessage(HttpMethod.Post, assignUri)
            {
                Content = new StringContent($"[{roleJson}]", System.Text.Encoding.UTF8, "application/json")
            };
            assignRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var assignResponse = await _httpClient.SendAsync(assignRequest, cancellationToken);
            if (!assignResponse.IsSuccessStatusCode)
            {
                var body = await assignResponse.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to assign role '{roleName}' to user '{userId}': {(int)assignResponse.StatusCode}: {body}");
            }
        }

        private async Task<string> GetAdminAccessTokenAsync(CancellationToken cancellationToken)
        {
            var tokenRequestUri = $"{NormalizeBaseUrl(_options.EffectiveInternalBaseUrl)}/realms/{_options.Realm}/protocol/openid-connect/token";
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

        public async Task<IEnumerable<UserDto>> GetUsersAsync(string? searchTerm = null, string? role = null, CancellationToken cancellationToken = default)
        {
            EnsureConfigured();

            var accessToken = await GetAdminAccessTokenAsync(cancellationToken);
            var baseUrl = NormalizeBaseUrl(_options.EffectiveInternalBaseUrl);

            // 1. Fetch all users — this endpoint works with the current service-account permissions.
            //    The /roles/{name}/users endpoint requires additional Keycloak privileges (manage-realm)
            //    that are not granted to the service account, so we avoid it entirely.
            var allUsersUri = $"{baseUrl}/admin/realms/{_options.Realm}/users?max=1000&enabled=true";
            var keycloakUsers = await GetUsersFromUriAsync(allUsersUri, accessToken, cancellationToken);

            // 2. Resolve roles for each user with bounded concurrency via /users/{id}/role-mappings/realm.
            //    This endpoint only requires view-users / manage-users, which are already granted.
            const int maxConcurrency = 10;
            using var throttler = new SemaphoreSlim(maxConcurrency);

            var roleTasks = keycloakUsers.Select(async ku =>
            {
                await throttler.WaitAsync(cancellationToken);
                try
                {
                    var roleMappingsUri = $"{baseUrl}/admin/realms/{_options.Realm}/users/{ku.Id}/role-mappings/realm";
                    var roles = await GetUserRolesAsync(roleMappingsUri, accessToken, cancellationToken);

                    var resolvedRole = roles.Any(r => string.Equals(r.Name, "admin", StringComparison.OrdinalIgnoreCase))
                        ? "admin"
                        : roles.Any(r => string.Equals(r.Name, "operator", StringComparison.OrdinalIgnoreCase))
                            ? "operator"
                            : "player";

                    return (User: ku, Role: resolvedRole);
                }
                finally
                {
                    throttler.Release();
                }
            }).ToList();

            var resolved = await Task.WhenAll(roleTasks);

            // 3. Apply role filter (done here because fetching by role requires manage-realm).
            IEnumerable<(KeycloakUserRepresentation User, string Role)> filtered = resolved;
            if (!string.IsNullOrWhiteSpace(role))
            {
                var normRole = role.Trim().ToLowerInvariant();
                filtered = filtered.Where(x => string.Equals(x.Role, normRole, StringComparison.OrdinalIgnoreCase));
            }

            // 4. Apply search filter.
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim();
                filtered = filtered.Where(x =>
                    (!string.IsNullOrEmpty(x.User.FirstName) && x.User.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.User.Username) && x.User.Username.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(x.User.Email) && x.User.Email.Contains(term, StringComparison.OrdinalIgnoreCase))
                );
            }

            // 5. Map to DTOs.
            var result = filtered.Select(x =>
            {
                var ku = x.User;
                var createdAt = DateTimeOffset.FromUnixTimeMilliseconds(ku.CreatedTimestamp).UtcDateTime;

                if (!Guid.TryParse(ku.Id, out var userGuid))
                {
                    throw new InvalidOperationException($"Keycloak returned an invalid non-GUID user id: '{ku.Id}'.");
                }

                var name = !string.IsNullOrWhiteSpace(ku.FirstName) ? ku.FirstName : ku.Username;
                if (string.IsNullOrWhiteSpace(name))
                    name = ku.Email;

                return new UserDto(userGuid, ku.Id, ku.Email, name, x.Role, createdAt);
            });

            return result.OrderByDescending(u => u.CreatedAt).ToList();
        }

        private async Task<List<KeycloakRoleRepresentation>> GetUserRolesAsync(string uri, string accessToken, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // If we cannot fetch roles for this user, treat them as a player rather than failing.
                return new List<KeycloakRoleRepresentation>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<KeycloakRoleRepresentation>>(SerializerOptions, cancellationToken);
            return result ?? new List<KeycloakRoleRepresentation>();
        }

        private async Task<List<KeycloakUserRepresentation>> GetUsersFromUriAsync(string uri, string accessToken, CancellationToken cancellationToken)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to fetch users from Keycloak: {(int)response.StatusCode}: {body}");
            }

            var result = await response.Content.ReadFromJsonAsync<List<KeycloakUserRepresentation>>(SerializerOptions, cancellationToken);
            return result ?? new List<KeycloakUserRepresentation>();
        }

        private sealed class KeycloakTokenResponse
        {
            [JsonPropertyName("access_token")]
            public string AccessToken { get; set; } = string.Empty;
        }

        private sealed class KeycloakUserRepresentation
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("username")]
            public string Username { get; set; } = string.Empty;

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("firstName")]
            public string FirstName { get; set; } = string.Empty;

            [JsonPropertyName("lastName")]
            public string LastName { get; set; } = string.Empty;

            [JsonPropertyName("enabled")]
            public bool Enabled { get; set; }

            [JsonPropertyName("createdTimestamp")]
            public long CreatedTimestamp { get; set; }
        }
        private sealed class KeycloakRoleRepresentation
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;
        }
    }
}
