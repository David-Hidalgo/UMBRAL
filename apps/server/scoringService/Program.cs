using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using ScoringService.Application.Abstractions;
using ScoringService.Application.Commands;
using ScoringService.Application.Common;
using ScoringService.Application.Handlers;
using ScoringService.Application.Validators;
using ScoringService.Domain.Exceptions;
using ScoringService.Domain.Repositories;
using ScoringService.Domain.Scoring;
using ScoringService.Infrastructure.Auth;
using ScoringService.Infrastructure.Clients;
using ScoringService.Infrastructure.Messaging;
using ScoringService.Infrastructure.Messaging.Consumers;
using ScoringService.Infrastructure.Notifications;
using ScoringService.Infrastructure.Notifications.Hubs;
using ScoringService.Infrastructure.Persistence.Contexts;
using ScoringService.Infrastructure.Persistence.Repositories;
using shared.Authentication;
using shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGenWithAuthSupport(builder.Configuration);

// --- Autenticación Keycloak (JWT) ---
builder.Services.Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"));
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

        if (!string.IsNullOrWhiteSpace(keycloakOptions.BaseUrl) && !string.IsNullOrWhiteSpace(keycloakOptions.Realm))
        {
            var publicIssuer = $"{keycloakOptions.BaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            var internalIssuer = $"{keycloakOptions.EffectiveInternalBaseUrl.TrimEnd('/')}/realms/{keycloakOptions.Realm}";
            options.TokenValidationParameters.ValidIssuers = [publicIssuer, internalIssuer];
        }

        var internalBase = keycloakOptions.EffectiveInternalBaseUrl;
        var publicBase = keycloakOptions.BaseUrl;

        if (!string.IsNullOrWhiteSpace(internalBase) &&
            !string.Equals(internalBase, publicBase, StringComparison.OrdinalIgnoreCase))
        {
            options.BackchannelHttpHandler = new KeycloakInternalRedirectHandler(publicBase, internalBase);
        }

        // Permite recibir el token por query string en la conexión WebSocket de SignalR.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

// --- Persistencia ---
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection");
builder.Services.AddDbContext<ScoringDbContext>(options => options.UseNpgsql(connectionString));

// --- MediatR + validaciones ---
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(SubmitEvidenceCommandHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddTransient<IValidator<SubmitEvidenceCommand>, SubmitEvidenceCommandValidator>();
builder.Services.AddTransient<IValidator<ValidateEvidenceCommand>, ValidateEvidenceCommandValidator>();
builder.Services.AddTransient<IValidator<ApplyPenaltyCommand>, ApplyPenaltyCommandValidator>();

// --- Cliente HTTP a missionService (contenido de etapas) ---
// Identidad de servicio (client credentials) para leer el contenido de misiones sin el token del jugador.
builder.Services.Configure<KeycloakServiceOptions>(options =>
{
    options.BaseUrl = keycloakOptions.EffectiveInternalBaseUrl;
    options.Realm = keycloakOptions.Realm;
    options.ClientId = builder.Configuration["Keycloak:ServiceClientId"] ?? "scoring-service";
    options.ClientSecret = builder.Configuration["Keycloak:ServiceClientSecret"] ?? "";
});
builder.Services.AddHttpClient<IServiceTokenProvider, KeycloakServiceTokenProvider>();

builder.Services.AddHttpClient<IMissionContentClient, MissionContentClient>();

// --- Mensajería RabbitMQ ---
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
builder.Services.AddHostedService<SessionEventsConsumer>();

// --- SignalR (tiempo real) ---
builder.Services.AddSignalR();
builder.Services.AddScoped<IScoringNotificationService, ScoringNotificationService>();

// --- Estrategias de puntaje por dificultad (patrón Strategy) ---
builder.Services.AddSingleton<IScoringStrategy, BeginnerScoringStrategy>();
builder.Services.AddSingleton<IScoringStrategy, IntermediateScoringStrategy>();
builder.Services.AddSingleton<IScoringStrategy, AdvancedScoringStrategy>();
builder.Services.AddSingleton<IScoringStrategyResolver, ScoringStrategyResolver>();

// --- Repositorios y servicios de aplicación ---
builder.Services.AddScoped<IScoringRepository, ScoringRepository>();
builder.Services.AddScoped<ISessionMonitorRepository, SessionMonitorRepository>();
builder.Services.AddScoped<IScoreBroadcaster, ScoreBroadcaster>();

builder.Services.AddHealthChecks().AddDbContextCheck<ScoringDbContext>();

var app = builder.Build();

// --- Manejo global de excepciones (RFC 7807 Problem Details, backend-spec §3) ---
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/problem+json";

        var (status, title, detail) = MapException(exception);
        context.Response.StatusCode = status;

        object payload = exception is ValidationException validationEx
            ? new
            {
                type = "https://httpstatuses.io/400",
                title,
                status,
                errors = validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage })
            }
            : new
            {
                type = $"https://httpstatuses.io/{status}",
                title,
                status,
                detail
            };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1"));
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.MapHub<ScoringHub>("/hubs/scoring");

// Migración automática en Development.
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<ScoringDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al migrar la base de datos de scoringService.");
    }
}

app.Run();

// Mapea excepciones de dominio a códigos HTTP semánticos.
static (int Status, string Title, string Detail) MapException(Exception? exception) => exception switch
{
    ValidationException => (StatusCodes.Status400BadRequest, "Error de validación.", "Uno o más campos no son válidos."),
    SessionMonitorNotFoundException e => (StatusCodes.Status404NotFound, "Sesión no encontrada.", e.Message),
    SubmissionNotFoundException e => (StatusCodes.Status404NotFound, "Evidencia no encontrada.", e.Message),
    TeamNotInSessionException e => (StatusCodes.Status404NotFound, "Equipo no encontrado en la sesión.", e.Message),
    SessionNotAcceptingEvidenceException e => (StatusCodes.Status409Conflict, "La sesión no acepta evidencias.", e.Message),
    SubmissionAlreadyValidatedException e => (StatusCodes.Status409Conflict, "La evidencia ya fue validada.", e.Message),
    SubStageAlreadySolvedException e => (StatusCodes.Status409Conflict, "Reto ya resuelto.", e.Message),
    TriviaAlreadyAnsweredException e => (StatusCodes.Status409Conflict, "Pregunta ya respondida.", e.Message),
    DomainException e => (StatusCodes.Status400BadRequest, "Solicitud inválida.", e.Message),
    _ => (StatusCodes.Status500InternalServerError, "Error interno del servidor.", "Ocurrió un error inesperado.")
};

// Redirige el tráfico backchannel de Keycloak del host público al host interno del contenedor.
public class KeycloakInternalRedirectHandler(string publicBaseUrl, string internalBaseUrl) : HttpClientHandler
{
    private readonly Uri _publicBase = new(publicBaseUrl.TrimEnd('/'));
    private readonly Uri _internalBase = new(internalBaseUrl.TrimEnd('/'));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null && request.RequestUri.Host.Equals(_publicBase.Host, StringComparison.OrdinalIgnoreCase))
        {
            var uriBuilder = new UriBuilder(request.RequestUri)
            {
                Scheme = _internalBase.Scheme,
                Host = _internalBase.Host,
                Port = _internalBase.Port
            };
            request.RequestUri = uriBuilder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}

// Expone Program para WebApplicationFactory en pruebas de integración.
public partial class Program { }
