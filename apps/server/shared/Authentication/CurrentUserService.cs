using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace shared.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    private const string SubjectClaim = "sub";
    private const string PreferredUsernameClaim = "preferred_username";
    private const string NameClaim = "name";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => AuthenticatedIdentity is not null;

    public string? UserId => FindFirstValue(ClaimTypes.NameIdentifier, SubjectClaim);

    public string? UserName => FindFirstValue(ClaimTypes.Name, PreferredUsernameClaim, NameClaim);

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var identity = AuthenticatedIdentity;
            if (identity is null)
                return Array.Empty<string>();

            return identity
                .FindAll(identity.RoleClaimType)
                .Select(claim => claim.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    /// <summary>
    /// Unico punto de acceso al contexto. El null-conditional es deliberado:
    /// fuera de una peticion HTTP (consumers de RabbitMQ) HttpContext es null.
    /// </summary>
    private ClaimsIdentity? AuthenticatedIdentity
        => _httpContextAccessor.HttpContext?.User.Identity is ClaimsIdentity { IsAuthenticated: true } identity
            ? identity
            : null;

    private string? FindFirstValue(params string[] claimTypes)
    {
        var identity = AuthenticatedIdentity;
        if (identity is null)
            return null;

        return claimTypes
            .Select(claimType => identity.FindFirst(claimType)?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
