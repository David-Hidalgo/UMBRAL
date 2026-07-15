using System.Text.Json;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using LiveSessionService.Application.Common;
using LiveSessionService.Application.Commands;
using LiveSessionService.Application.Handlers;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Application.Validators;
using LiveSessionService.Domain.Exceptions;
using LiveSessionService.Domain.Repositories;
using LiveSessionService.Infrastructure.Clients;
using LiveSessionService.Infrastructure.Notifications.Hubs;
using LiveSessionService.Infrastructure.Messaging;
using LiveSessionService.Infrastructure.Persistence;
using LiveSessionService.Infrastructure.Notifications;
using shared.Authentication;
using shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGenWithAuthSupport(builder.Configuration);

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

        // Configuración para recibir token en SignalR (query string)
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

// DB Context Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<LiveSessionDbContext>(options =>
    options.UseNpgsql(connectionString));

// MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateLiveSessionHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation: Manual Registration
builder.Services.AddTransient<IValidator<CreateLiveSessionCommand>, CreateLiveSessionCommandValidator>();
builder.Services.AddTransient<IValidator<CreateTeamCommand>, CreateTeamCommandValidator>();
builder.Services.AddTransient<IValidator<JoinTeamCommand>, JoinTeamCommandValidator>();
builder.Services.AddTransient<IValidator<TransitionSessionCommand>, TransitionSessionCommandValidator>();
builder.Services.AddTransient<IValidator<AdvanceStageCommand>, AdvanceStageCommandValidator>();
builder.Services.AddTransient<IValidator<RequestHintCommand>, RequestHintCommandValidator>();
builder.Services.AddTransient<IValidator<GrantHintCommand>, GrantHintCommandValidator>();

// HttpClient registration
builder.Services.AddHttpClient<IMissionServiceClient, MissionServiceClient>();

// RabbitMQ Event Bus configuration
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();

// SignalR Hub context notification service
builder.Services.AddSignalR();
builder.Services.AddScoped<ISessionNotificationService, SessionNotificationService>();

// Repositories
builder.Services.AddScoped<ILiveSessionRepository, LiveSessionRepository>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LiveSessionDbContext>();

var app = builder.Build();

// Global Exception Handler
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = feature?.Error;

        context.Response.ContentType = "application/json";

        if (exception is ValidationException validationEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errors = validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { errors }));
            return;
        }

        if (exception is BusinessException businessEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = businessEx.Message }));
            return;
        }

        if (exception is ArgumentException argEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = argEx.Message }));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Ocurrió un error inesperado en el servidor." }));
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
app.MapHub<LiveSessionHub>("/hubs/sessions");

// Migrate database on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<LiveSessionDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "Ocurrió un error al migrar la base de datos de liveSessionService.");
        }
    }
}

app.Run();

// Re-use KeycloakInternalRedirectHandler from missionService
public class KeycloakInternalRedirectHandler(string publicBaseUrl, string internalBaseUrl) : HttpClientHandler
{
    private readonly Uri _publicBase = new(publicBaseUrl.TrimEnd('/'));
    private readonly Uri _internalBase = new(internalBaseUrl.TrimEnd('/'));

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is not null && request.RequestUri.Host.Equals(_publicBase.Host, StringComparison.OrdinalIgnoreCase))
        {
            var builder = new UriBuilder(request.RequestUri)
            {
                Scheme = _internalBase.Scheme,
                Host = _internalBase.Host,
                Port = _internalBase.Port
            };
            request.RequestUri = builder.Uri;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
