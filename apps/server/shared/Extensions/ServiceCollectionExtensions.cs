using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using shared.Authentication;

namespace shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSwaggerGenWithAuthSupport(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var keycloak = configuration.GetSection("Keycloak").Get<KeycloakOptions>() ?? new KeycloakOptions();

        string? authorizationUrl = null;
        string? tokenUrl = null;

        if (!string.IsNullOrWhiteSpace(keycloak.BaseUrl) && !string.IsNullOrWhiteSpace(keycloak.Realm))
        {
            authorizationUrl = $"{keycloak.BaseUrl.TrimEnd('/')}/realms/{keycloak.Realm}/protocol/openid-connect/auth";
            tokenUrl = $"{keycloak.BaseUrl.TrimEnd('/')}/realms/{keycloak.Realm}/protocol/openid-connect/token";
        }

        var scopes = new[] { "openid", "profile" };

        if (!Uri.TryCreate(authorizationUrl, UriKind.Absolute, out var authUri) ||
            !Uri.TryCreate(tokenUrl, UriKind.Absolute, out var tokenUri))
        {
            services.AddSwaggerGen(o => o.CustomSchemaIds(id => id.FullName!.Replace('+', '-')));
            return services;
        }

        services.AddSwaggerGen(o =>
        {
            o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

            o.AddSecurityDefinition("Keycloak", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = authUri,
                        TokenUrl = tokenUri,
                        Scopes = scopes.ToDictionary(s => s, s => s)
                    }
                }
            });

            o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Keycloak", doc),
                    []
                }
            });
        });

        return services;
    }
}