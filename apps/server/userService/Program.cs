using System.Text.Json;
using FluentValidation;
using MediatR;
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

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreateUserHandler).Assembly);
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation — registro manual para evitar dependencia adicional
builder.Services.AddTransient<IValidator<UserService.Application.Commands.CreateUserCommand>, CreateUserCommandValidator>();

// Infrastructure bindings
builder.Services.AddScoped<IUserRepository, InMemoryUserRepository>();
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
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
