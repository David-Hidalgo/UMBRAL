using System.Text.Json;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using UserService.Application.Behaviors;
using UserService.Application.Handlers;
using UserService.Application.Interfaces;
using UserService.Application.Validators;
using UserService.Domain.RepositoryInterfaces;
using UserService.Infrastructure.Authentication;
using UserService.Infrastructure.Messaging;
using UserService.Infrastructure.Persistence;
using shared.Authentication;
using shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGenWithAuthSupport(builder.Configuration);

builder.Services.Configure<KeycloakAdminOptions>(builder.Configuration.GetSection("Keycloak"));
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

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation — registro manual para evitar dependencia adicional
builder.Services.AddTransient<IValidator<UserService.Application.Commands.CreateUserCommand>, CreateUserCommandValidator>();
builder.Services.AddTransient<IValidator<UserService.Application.Commands.DeleteUserCommand>, DeleteUserCommandValidator>();
builder.Services.AddTransient<IValidator<UserService.Application.Queries.GetUsersQuery>, GetUsersQueryValidator>();

// Infrastructure bindings
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddScoped<IEventBus, NullEventBus>();
builder.Services.AddHttpClient<IIdentityProviderService, KeycloakUserProvisioningService>();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Global exception handler — convierte ValidationException en 400, resto en 500
app.UseExceptionHandler(exceptionApp =>
{
    exceptionApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is ValidationException validationEx)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var errors = validationEx.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage });
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { errors }));
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
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

/// <summary>
/// HTTP handler that rewrites backchannel requests from the public Keycloak URL
/// (e.g. http://localhost:8080) to the internal container hostname
/// (e.g. http://keycloak:8080). This allows the JWT middleware's standard
/// two-step flow (fetch discovery doc → fetch JWKS) to work inside Docker/Podman
/// without requiring a custom IConfigurationRetriever.
/// </summary>
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
