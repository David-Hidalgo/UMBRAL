namespace shared.Authentication;

public class KeycloakOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Internal base URL used by containers to reach Keycloak inside the Docker network
    /// (e.g. http://keycloak:8080). When set, metadata and JWKS are fetched from this
    /// address instead of <see cref="BaseUrl"/>, avoiding localhost resolution issues.
    /// Falls back to <see cref="BaseUrl"/> when empty.
    /// </summary>
    public string InternalBaseUrl { get; set; } = string.Empty;

    public string Realm { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    /// <summary>Effective base URL for server-side OIDC/JWKS requests.</summary>
    public string EffectiveInternalBaseUrl =>
        string.IsNullOrWhiteSpace(InternalBaseUrl) ? BaseUrl : InternalBaseUrl;
}