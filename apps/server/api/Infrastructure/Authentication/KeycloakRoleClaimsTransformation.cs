using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using shared.Authentication;

namespace api.Infrastructure.Authentication;

public sealed class KeycloakRoleClaimsTransformation : IClaimsTransformation
{
    private const string RealmAccessClaim = "realm_access";
    private const string ResourceAccessClaim = "resource_access";
    private const string RolesClaim = "roles";

    private readonly KeycloakOptions _options;

    public KeycloakRoleClaimsTransformation(IOptions<KeycloakOptions> options)
    {
        _options = options.Value;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var existingRoles = new HashSet<string>(
            identity.FindAll(identity.RoleClaimType).Select(claim => claim.Value),
            StringComparer.OrdinalIgnoreCase);

        AddRolesFromRealmAccess(principal, identity, existingRoles);
        AddRolesFromResourceAccess(principal, identity, existingRoles);

        return Task.FromResult(principal);
    }

    private static void AddRole(ClaimsIdentity identity, HashSet<string> existingRoles, string? role)
    {
        if (string.IsNullOrWhiteSpace(role) || !existingRoles.Add(role))
            return;

        identity.AddClaim(new Claim(identity.RoleClaimType, role));
    }

    private static void AddRolesFromClaim(string? claimValue, ClaimsIdentity identity, HashSet<string> existingRoles)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
            return;

        using var document = JsonDocument.Parse(claimValue);
        if (!document.RootElement.TryGetProperty(RolesClaim, out var rolesElement) ||
            rolesElement.ValueKind != JsonValueKind.Array)
            return;

        foreach (var roleElement in rolesElement.EnumerateArray())
            AddRole(identity, existingRoles, roleElement.GetString());
    }

    private void AddRolesFromRealmAccess(ClaimsPrincipal principal, ClaimsIdentity identity, HashSet<string> existingRoles)
    {
        var realmAccess = principal.FindFirst(RealmAccessClaim)?.Value;
        AddRolesFromClaim(realmAccess, identity, existingRoles);
    }

    private void AddRolesFromResourceAccess(ClaimsPrincipal principal, ClaimsIdentity identity, HashSet<string> existingRoles)
    {
        var resourceAccess = principal.FindFirst(ResourceAccessClaim)?.Value;
        if (string.IsNullOrWhiteSpace(resourceAccess))
            return;

        using var document = JsonDocument.Parse(resourceAccess);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
            return;

        if (!string.IsNullOrWhiteSpace(_options.Audience) &&
            document.RootElement.TryGetProperty(_options.Audience, out var audienceElement))
        {
            AddRolesFromClaim(audienceElement.GetRawText(), identity, existingRoles);
            return;
        }

        foreach (var clientElement in document.RootElement.EnumerateObject())
            AddRolesFromClaim(clientElement.Value.GetRawText(), identity, existingRoles);
    }
}
