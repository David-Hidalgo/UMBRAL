using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using LiveSessionService.Application.DTOs;
using LiveSessionService.Application.Abstractions;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Infrastructure.Clients
{
    public class MissionServiceClient : IMissionServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        public MissionServiceClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _baseUrl = configuration["MissionService:BaseUrl"] ?? "http://missionservice:5022";
        }

        public async Task<MissionClientDto?> GetMissionByIdAsync(Guid id, string jwtToken, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_baseUrl))
            {
                throw new InvalidOperationException("La URL base de MissionService no está configurada.");
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/api/missions/{id}");

            if (!string.IsNullOrWhiteSpace(jwtToken))
            {
                // Limpiar prefijo "Bearer " si viniera incluido
                var cleanToken = jwtToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? jwtToken.Substring(7).Trim()
                    : jwtToken.Trim();

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cleanToken);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new BusinessException($"Error al consultar el microservicio de misiones: {response.StatusCode} - {body}");
            }

            try
            {
                return JsonSerializer.Deserialize<MissionClientDto>(body, SerializerOptions);
            }
            catch (JsonException ex)
            {
                throw new BusinessException($"El microservicio de misiones devolvió una respuesta no válida: {body}", ex);
            }
        }
    }
}
