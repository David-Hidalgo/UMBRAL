using System;

namespace LiveSessionService.Domain.Entities
{
    public class Player
    {
        public Guid Id { get; private set; }
        public string KeycloakUserId { get; private set; } = null!;
        public string DisplayName { get; private set; } = null!;
        public Guid TeamId { get; private set; }
        public DateTime JoinedAt { get; private set; }

        // Requerido por EF Core
        private Player() { }

        public static Player Create(Guid id, string keycloakUserId, string displayName, Guid teamId)
        {
            if (string.IsNullOrWhiteSpace(keycloakUserId))
                throw new ArgumentException("El ID de Keycloak del jugador es requerido.", nameof(keycloakUserId));

            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("El nombre visible del jugador es requerido.", nameof(displayName));

            return new Player
            {
                Id = id,
                KeycloakUserId = keycloakUserId,
                DisplayName = displayName,
                TeamId = teamId,
                JoinedAt = DateTime.UtcNow
            };
        }
    }
}
