using System;

namespace MissionService.Domain.Entities
{
    public class Clue
    {
        public Guid Id { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public int Order { get; private set; }
        public Guid TreasureHuntSubStageId { get; private set; }

        private Clue() { }

        public static Clue Create(Guid id, string text, int order)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Clue ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Clue text cannot be empty.", nameof(text));
            if (order < 0)
                throw new ArgumentException("Clue order cannot be negative.", nameof(order));

            return new Clue
            {
                Id = id,
                Text = text,
                Order = order
            };
        }
    }
}
