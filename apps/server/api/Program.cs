using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using api.Application.Exceptions;
using api.Application.Interfaces;
using api.Infrastructure.Clients;
using MediatR;
using Microsoft.Extensions.Options;
using shared.Authentication;
using shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGenWithAuthSupport(builder.Configuration);
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
builder.Services.Configure<UserServiceOptions>(builder.Configuration.GetSection("UserService"));
builder.Services.Configure<MissionServiceOptions>(builder.Configuration.GetSection("MissionService"));

var keycloakOptions = builder.Configuration.GetSection("Keycloak").Get<KeycloakOptions>() ?? new KeycloakOptions();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (!string.IsNullOrWhiteSpace(keycloakOptions.BaseUrl) && !string.IsNullOrWhiteSpace(keycloakOptions.Realm))
        {
            options.Authority = $"{keycloakOptions.BaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            options.MetadataAddress = $"{keycloakOptions.BaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}/.well-known/openid-configuration";
        }

        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        if (!string.IsNullOrWhiteSpace(keycloakOptions.Audience))
            options.Audience = keycloakOptions.Audience;

        // Accept both the internal container issuer and the public localhost issuer.
        // Tokens are minted with the public KC_HOSTNAME (localhost:8080) as issuer,
        // but in some configs the discovery doc may advertise the internal hostname.
        if (!string.IsNullOrWhiteSpace(keycloakOptions.BaseUrl) && !string.IsNullOrWhiteSpace(keycloakOptions.Realm))
        {
            var publicIssuer   = $"{keycloakOptions.BaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            var internalIssuer = $"{keycloakOptions.EffectiveInternalBaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            options.TokenValidationParameters.ValidIssuers = [publicIssuer, internalIssuer];
        }

        // When running inside Docker, the gateway cannot reach localhost:8080 for
        // backchannel calls (discovery doc + JWKS fetch). Replace the host with the
        // internal container name (keycloak:8080) transparently at the HTTP level so
        // the normal two-step flow (discover → fetch JWKS → populate SigningKeys) works.
        var internalBase = keycloakOptions.EffectiveInternalBaseUrl;
        var publicBase   = keycloakOptions.BaseUrl;

        if (!string.IsNullOrWhiteSpace(internalBase) &&
            !string.Equals(internalBase, publicBase, StringComparison.OrdinalIgnoreCase))
        {
            options.BackchannelHttpHandler = new KeycloakInternalRedirectHandler(publicBase, internalBase);
        }
    });

builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy =>
    {
        var allowedOrigins =
            builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? [];

        if (allowedOrigins.Length == 0)
            allowedOrigins = ["http://localhost:3000"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<UserServiceOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        client.BaseAddress = new Uri(opts.BaseUrl);
});

builder.Services.AddHttpClient<IMissionServiceClient, MissionServiceClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<MissionServiceOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        client.BaseAddress = new Uri(opts.BaseUrl);
});

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHealthChecks();

var app = builder.Build();

// Global exception handler — 502 para errores del microservicio downstream, 500 para el resto
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is UserServiceException || feature?.Error is MissionServiceException)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Upstream service error." }));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.UseHttpsRedirection();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

/// <summary>
/// HTTP handler that rewrites backchannel requests from the public Keycloak URL
/// (e.g. http://localhost:8080) to the internal container hostname
/// (e.g. http://keycloak:8080). This allows the JWT middleware's standard
/// two-step flow (fetch discovery doc → fetch JWKS) to work inside Docker
/// without requiring a custom IConfigurationRetriever.
/// </summary>
public class KeycloakInternalRedirectHandler(string publicBaseUrl, string internalBaseUrl) : HttpClientHandler
{
    private readonly Uri _publicBase   = new(publicBaseUrl.TrimEnd('/'));
    private readonly Uri _internalBase = new(internalBaseUrl.TrimEnd('/'));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null && request.RequestUri.Host.Equals(_publicBase.Host, StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Scheme = _internalBase.Scheme,
                Host   = _internalBase.Host,
                Port   = _internalBase.Port
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}

public partial class Program { }
