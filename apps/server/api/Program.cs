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
builder.Services.Configure<LiveSessionServiceOptions>(builder.Configuration.GetSection("LiveSessionService"));

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

        // Acepta tanto el emisor del contenedor interno como el emisor público de localhost.
        // Los tokens se emiten con el KC_HOSTNAME público (localhost:8080) como emisor,
        // pero en algunas configuraciones el documento de descubrimiento puede anunciar el nombre de host interno.
        if (!string.IsNullOrWhiteSpace(keycloakOptions.BaseUrl) && !string.IsNullOrWhiteSpace(keycloakOptions.Realm))
        {
            var publicIssuer   = $"{keycloakOptions.BaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            var internalIssuer = $"{keycloakOptions.EffectiveInternalBaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            options.TokenValidationParameters.ValidIssuers = [publicIssuer, internalIssuer];
        }

        // Al ejecutar dentro de Docker, el gateway no puede alcanzar localhost:8080 para las
        // llamadas de backchannel (documento de descubrimiento + obtención de JWKS). Reemplaza el host con el
        // nombre del contenedor interno (keycloak:8080) de forma transparente a nivel de HTTP para que
        // el flujo normal de dos pasos (descubrir → obtener JWKS → poblar SigningKeys) funcione.
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
        {
            // Fallback por si no hay config: web (3000) y Expo web (8081)
            allowedOrigins = builder.Environment.IsDevelopment()
                ? ["http://localhost:3000", "http://localhost:8081"]
                : ["http://localhost:3000"];
        }

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Requerido por SignalR — incompatible con AllowAnyOrigin()
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

builder.Services.AddHttpClient<ILiveSessionServiceClient, LiveSessionServiceClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<LiveSessionServiceOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
        client.BaseAddress = new Uri(opts.BaseUrl);
});

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddHealthChecks();

var app = builder.Build();

// Global exception handler — 502 para errores del microservicio downstream, 500 para el resto
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is UserServiceException usEx)
        {
            context.Response.StatusCode = usEx.StatusCode;
            context.Response.ContentType = "application/json";
            var responseBody = !string.IsNullOrWhiteSpace(usEx.Content) 
                ? usEx.Content 
                : JsonSerializer.Serialize(new { error = usEx.Message });
            await context.Response.WriteAsync(responseBody);
            return;
        }

        if (feature?.Error is MissionServiceException msEx)
        {
            context.Response.StatusCode = msEx.StatusCode;
            context.Response.ContentType = "application/json";
            var responseBody = !string.IsNullOrWhiteSpace(msEx.Content) 
                ? msEx.Content 
                : JsonSerializer.Serialize(new { error = msEx.Message });
            await context.Response.WriteAsync(responseBody);
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
app.MapReverseProxy();

app.Run();

/// <summary>
/// Handler HTTP que reescribe las solicitudes de backchannel desde la URL pública de Keycloak
/// (ej. http://localhost:8080) al nombre de host del contenedor interno (ej. http://keycloak:8080).
/// Esto permite que el flujo estándar de dos pasos del middleware JWT (obtener documento de
/// descubrimiento → obtener JWKS) funcione dentro de Docker sin requerir un IConfigurationRetriever personalizado.
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
