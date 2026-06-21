using System;
using System.Collections.Generic;
using System.Linq;
using LiveSessionService.Domain.Exceptions;

namespace LiveSessionService.Domain.Entities
{
    public class Team
    {
        private readonly List<Player> _players = new();

        public Guid Id { get; private set; }
        public string Name { get; private set; } = null!;
        public Guid LiveSessionId { get; private set; }
        public IReadOnlyCollection<Player> Players => _players.AsReadOnly();

        // Requerido por EF Core
        private Team() { }

        public static Team Create(Guid id, string name, Guid liveSessionId)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("El nombre del equipo es requerido.", nameof(name));

            return new Team
            {
                Id = id,
                Name = name,
                LiveSessionId = liveSessionId
            };
        }

        public void AddPlayer(Player player)
        {
            if (player == null)
                throw new ArgumentNullException(nameof(player));

            if (_players.Any(p => p.KeycloakUserId == player.KeycloakUserId))
                throw new BusinessException($"El jugador ya está registrado en el equipo '{Name}'.");

            _players.Add(player);
        }
    }
}
