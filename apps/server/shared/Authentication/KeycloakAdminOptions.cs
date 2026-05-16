namespace shared.Authentication;

public sealed class KeycloakAdminOptions : KeycloakOptions
{
    public string AdminClientId { get; set; } = string.Empty;
    public string AdminClientSecret { get; set; } = string.Empty;
}