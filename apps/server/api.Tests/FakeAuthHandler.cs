using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace api.Tests;

/// <summary>
/// Handler de autenticación falso para pruebas de integración.
/// Interpreta el token Bearer como una lista de roles separados por coma.
/// Ejemplos:
///   Bearer ""          → usuario autenticado sin roles
///   Bearer "admin"     → usuario con rol "admin"
///   Bearer "admin,operator" → usuario con ambos roles
/// Si no hay header Authorization, el principal es anónimo (no autenticado).
/// </summary>
public class FakeAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var header = authHeader.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Invalid scheme"));

        var token = header["Bearer ".Length..].Trim();

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,           "testuser"),
            new(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
        };

        // Cada segmento no vacío del token es un rol
        foreach (var role in token.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity  = new ClaimsIdentity(claims, "Test", ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identity);
        var ticket    = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
