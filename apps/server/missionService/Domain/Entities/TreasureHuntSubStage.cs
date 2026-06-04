using System;
using System.Collections.Generic;

namespace MissionService.Domain.Entities
{
    public class TreasureHuntSubStage : SubStage
    {
        public string Objective { get; private set; } = string.Empty;
        public string SecretCode { get; private set; } = string.Empty;

        private TreasureHuntSubStage() : base() { }

        public static TreasureHuntSubStage Create(Guid id, string title, int order, int points, string objective, string secretCode, List<Clue>? clues)
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

            var th = new TreasureHuntSubStage
            {
                Id = id,
                Title = title,
                Order = order,
                Points = points,
                Objective = objective,
                SecretCode = secretCode.Trim().ToUpperInvariant()
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
    }
}
