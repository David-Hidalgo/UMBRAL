namespace shared.Authentication;

public class KeycloakOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}