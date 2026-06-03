using System;
using System.Collections.Generic;

namespace MissionService.Domain.Entities
{
    public class TreasureHuntSubStage : SubStage
    {
        public string Objective { get; private set; } = string.Empty;

        private readonly List<Clue> _clues = new();
        public IReadOnlyCollection<Clue> Clues => _clues.AsReadOnly();

        private TreasureHuntSubStage() : base() { }

        public static TreasureHuntSubStage Create(Guid id, string title, int order, int points, string objective, List<Clue> clues)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Substage ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Substage title cannot be empty.", nameof(title));
            if (points < 0)
                throw new ArgumentException("Substage points cannot be negative.", nameof(points));
            if (string.IsNullOrWhiteSpace(objective))
                throw new ArgumentException("Treasure hunt objective cannot be empty.", nameof(objective));
            if (clues == null || clues.Count == 0)
                throw new ArgumentException("Treasure hunt must contain at least one clue.", nameof(clues));

            var th = new TreasureHuntSubStage
            {
                Id = id,
                Title = title,
                Order = order,
                Points = points,
                Objective = objective
            };

            foreach (var clue in clues)
            {
                th.AddClue(clue);
            }

            return th;
        }

        private void AddClue(Clue clue)
        {
            if (clue == null)
                throw new ArgumentNullException(nameof(clue));
            _clues.Add(clue);
        }
    }
}
