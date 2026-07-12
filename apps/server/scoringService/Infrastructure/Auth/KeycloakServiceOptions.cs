namespace ScoringService.Infrastructure.Auth
{
    /// <summary>Configuración del cliente de servicio (client credentials) del scoring en Keycloak.</summary>
    public sealed class KeycloakServiceOptions
    {
        /// <summary>URL interna de Keycloak (backchannel), p. ej. http://keycloak:8080.</summary>
        public string BaseUrl { get; set; } = "http://keycloak:8080";
        public string Realm { get; set; } = "umbral";
        public string ClientId { get; set; } = "scoring-service";
        public string ClientSecret { get; set; } = "";
    }
}
