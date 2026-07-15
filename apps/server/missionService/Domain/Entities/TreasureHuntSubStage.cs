using System;
using System.Collections.Generic;

namespace MissionService.Domain.Entities
{
    public class TreasureHuntSubStage : SubStage
    {
        public string Objective { get; private set; } = string.Empty;
        public string SecretCode { get; private set; } = string.Empty;

        // Ubicación del tesoro y radio de búsqueda (opcionales). Se muestran en un mapa
        // Leaflet al operador (al crear la misión) y al jugador (al jugar la búsqueda).
        public double? Latitude { get; private set; }
        public double? Longitude { get; private set; }
        public int? SearchRadiusMeters { get; private set; }

        private TreasureHuntSubStage() : base() { }

        public static TreasureHuntSubStage Create(
            Guid id,
            string title,
            int order,
            int points,
            string objective,
            string secretCode,
            List<Clue>? clues,
            double? latitude = null,
            double? longitude = null,
            int? searchRadiusMeters = null)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Substage ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Substage title cannot be empty.", nameof(title));
            if (points < 0)
                throw new ArgumentException("Substage points cannot be negative.", nameof(points));
            if (string.IsNullOrWhiteSpace(objective))
                throw new ArgumentException("Treasure hunt objective cannot be empty.", nameof(objective));
            if (string.IsNullOrWhiteSpace(secretCode))
                throw new ArgumentException("Secret code cannot be empty.", nameof(secretCode));
            if (secretCode.Length > 100)
                throw new ArgumentException("Secret code cannot exceed 100 characters.", nameof(secretCode));
            if (!System.Text.RegularExpressions.Regex.IsMatch(secretCode, "^[a-zA-Z0-9-]+$"))
                throw new ArgumentException("Secret code must be alphanumeric and can contain hyphens.", nameof(secretCode));

            ValidateLocation(latitude, longitude, searchRadiusMeters);

            var th = new TreasureHuntSubStage
            {
                Id = id,
                Title = title,
                Order = order,
                Points = points,
                Objective = objective,
                SecretCode = secretCode.Trim().ToUpperInvariant(),
                Latitude = latitude,
                Longitude = longitude,
                SearchRadiusMeters = searchRadiusMeters
            };

            if (clues != null)
            {
                foreach (var clue in clues)
                {
                    th.AddClue(clue);
                }
            }

            return th;
        }

        public void UpdateDetails(
            string objective,
            string secretCode,
            double? latitude = null,
            double? longitude = null,
            int? searchRadiusMeters = null)
        {
            if (string.IsNullOrWhiteSpace(objective))
                throw new ArgumentException("Treasure hunt objective cannot be empty.", nameof(objective));
            if (string.IsNullOrWhiteSpace(secretCode))
                throw new ArgumentException("Secret code cannot be empty.", nameof(secretCode));
            if (secretCode.Length > 100)
                throw new ArgumentException("Secret code cannot exceed 100 characters.", nameof(secretCode));
            if (!System.Text.RegularExpressions.Regex.IsMatch(secretCode, "^[a-zA-Z0-9-]+$"))
                throw new ArgumentException("Secret code must be alphanumeric and can contain hyphens.", nameof(secretCode));

            ValidateLocation(latitude, longitude, searchRadiusMeters);

            Objective = objective;
            SecretCode = secretCode.Trim().ToUpperInvariant();
            Latitude = latitude;
            Longitude = longitude;
            SearchRadiusMeters = searchRadiusMeters;
        }

        // Latitud/longitud van juntas o ninguna; rangos geográficos válidos; radio positivo.
        private static void ValidateLocation(double? latitude, double? longitude, int? searchRadiusMeters)
        {
            if (latitude.HasValue != longitude.HasValue)
                throw new ArgumentException("Latitude and longitude must be provided together.");

            if (latitude.HasValue)
            {
                if (latitude.Value < -90 || latitude.Value > 90)
                    throw new ArgumentException("Latitude must be between -90 and 90.", nameof(latitude));
                if (longitude!.Value < -180 || longitude.Value > 180)
                    throw new ArgumentException("Longitude must be between -180 and 180.", nameof(longitude));
            }

            if (searchRadiusMeters.HasValue && searchRadiusMeters.Value <= 0)
                throw new ArgumentException("Search radius must be positive.", nameof(searchRadiusMeters));
        }
    }
}
