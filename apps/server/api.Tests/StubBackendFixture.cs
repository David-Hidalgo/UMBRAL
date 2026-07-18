using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests;

/// <summary>
/// Levanta el gateway con los tres clusters de YARP apuntando a un backend stub local.
///
/// Sin esto los tests no serían herméticos: YARP reenviaría a los puertos reales
/// (5020/5022/5026) y el resultado dependería de si el stack de Docker está encendido.
/// Con los microservicios apagados el proxy devuelve 502, y con ellos encendidos
/// devuelven 401 al no reconocer el token falso. En ambos casos se estaría midiendo
/// el downstream, no la autorización del gateway, que es lo que estas pruebas verifican.
///
/// El stub responde 200 a todo, de modo que un 200 significa exactamente
/// "el gateway autorizó la petición y la reenvió".
/// </summary>
public sealed class StubBackendFixture : IAsyncLifetime
{
    private static readonly string[] ClusterIds = ["usercluster", "missioncluster", "livesessioncluster"];

    private WebApplication _stub = null!;

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var stubBuilder = WebApplication.CreateBuilder();
        stubBuilder.WebHost.UseUrls("http://127.0.0.1:0"); // puerto libre asignado por el SO
        _stub = stubBuilder.Build();
        _stub.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("stub");
        });
        await _stub.StartAsync();

        var stubAddress = _stub.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.First();

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            foreach (var clusterId in ClusterIds)
            {
                builder.UseSetting(
                    $"ReverseProxy:Clusters:{clusterId}:Destinations:destination1:Address",
                    stubAddress);
            }

            builder.ConfigureServices(services =>
            {
                // Reemplaza el esquema JWT por uno de prueba que acepta cualquier
                // token "Bearer <roles-separados-por-coma>".
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>("Test", _ => { });
            });
        });

        // Fuerza la construcción del host antes de que corra el primer test.
        _ = Factory.Services;
    }

    public async Task DisposeAsync()
    {
        Factory?.Dispose();
        if (_stub is not null)
        {
            await _stub.StopAsync();
            await _stub.DisposeAsync();
        }
    }
}
