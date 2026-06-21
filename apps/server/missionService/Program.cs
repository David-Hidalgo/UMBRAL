using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MissionService.Application.Common;
using MissionService.Application.Handlers;
using MissionService.Application.Validators;
using MissionService.Domain.Exceptions;
using MissionService.Domain.Repositories;
using MissionService.Infrastructure.Persistence;
using MissionService.Application.Abstractions;
using MissionService.Infrastructure.Messaging;
using MissionService.Infrastructure.Messaging.Consumers;
using shared.Authentication;
using shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
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
    });

builder.Services.AddAuthorization();
builder.Services.AddTransient<IClaimsTransformation, KeycloakRoleClaimsTransformation>();

// DB Context Setup
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetValue<string>("ConnectionStrings:DefaultConnection");

builder.Services.AddDbContext<MissionDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateMissionHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation Manual Registration
builder.Services.AddTransient<IValidator<MissionService.Application.Commands.CreateMissionCommand>, CreateMissionCommandValidator>();
builder.Services.AddTransient<IValidator<MissionService.Application.Queries.GetMissionsQuery>, GetMissionsQueryValidator>();
builder.Services.AddTransient<IValidator<MissionService.Application.Queries.GetMissionByIdQuery>, GetMissionByIdQueryValidator>();
builder.Services.AddTransient<IValidator<MissionService.Application.Commands.UpdateMissionCommand>, UpdateMissionCommandValidator>();
builder.Services.AddTransient<IValidator<MissionService.Application.Commands.AddStageCommand>, AddStageCommandValidator>();
builder.Services.AddTransient<IValidator<MissionService.Application.Commands.UpdateStageCommand>, UpdateStageCommandValidator>();
builder.Services.AddTransient<IValidator<MissionService.Application.Commands.ReorderStagesCommand>, ReorderStagesCommandValidator>();

// Infrastructure
builder.Services.AddScoped<IMissionRepository, MissionRepository>();
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<IEventBus, RabbitMqEventBus>();
builder.Services.AddHostedService<SessionEventsConsumer>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<MissionDbContext>();

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

        if (exception is DuplicateMissionNameException duplicateEx)
        {
            context.Response.StatusCode = StatusCodes.Status409Conflict;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = duplicateEx.Message }));
            return;
        }

        if (exception is TriviaRequiresCorrectAnswerException triviaEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = triviaEx.Message }));
            return;
        }

        if (exception is MissionActiveException activeEx)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = activeEx.Message }));
            return;
        }

        if (exception is StageActiveException stageActiveEx)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = stageActiveEx.Message }));
            return;
        }

        if (exception is MissionHasActiveSessionsException hasSessionsEx)
        {
            context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = hasSessionsEx.Message }));
            return;
        }

        if (exception is MissionNotFoundException notFoundEx)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = notFoundEx.Message }));
            return;
        }

        if (exception is StageNotFoundException stageNotFoundEx)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = stageNotFoundEx.Message }));
            return;
        }

        if (exception is DomainException domainEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = domainEx.Message }));
            return;
        }

        if (exception is ArgumentException argEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = argEx.Message }));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
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
// Migrate database on startup (Development only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<MissionDbContext>();
            db.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database.");
        }
    }
}

app.Run();

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
