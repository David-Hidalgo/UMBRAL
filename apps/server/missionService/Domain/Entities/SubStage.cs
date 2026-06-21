using System;
using System.Collections.Generic;
using MissionService.Domain.ValueObjects;

namespace MissionService.Domain.Entities
{
    public abstract class SubStage
    {
        public Guid Id { get; protected set; }
        public string Title { get; protected set; } = string.Empty;
        public int Order { get; protected set; }
        public int Points { get; protected set; }
        public Guid StageId { get; protected set; }
        public Status Status { get; protected set; } = new Status(0);

        protected readonly List<Clue> _clues = new();
        public IReadOnlyCollection<Clue> Clues => _clues.AsReadOnly();

        protected SubStage() { }

        public void IncrementSessionCount() => Status = Status.Increment();
        public void DecrementSessionCount() => Status = Status.Decrement();

        public void Update(string title, int order, int points)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Substage title cannot be empty.", nameof(title));
            if (points < 0)
                throw new ArgumentException("Substage points cannot be negative.", nameof(points));

            Title = title;
            Order = order;
            Points = points;
        }

        public void UpdateClues(List<Clue> newClues)
        {
            // Eliminar las pistas que no estén en la nueva lista
            var cluesToRemove = _clues.Where(c => !newClues.Any(nc => nc.Id == c.Id)).ToList();
            foreach (var clue in cluesToRemove)
            {
                _clues.Remove(clue);
            }

            // Actualizar existente o agregar nueva
            foreach (var newClue in newClues)
            {
                var existing = _clues.FirstOrDefault(c => c.Id == newClue.Id);
                if (existing != null)
                {
                    existing.Update(newClue.Text, newClue.Order);
                }
                else
                {
                    AddClue(newClue);
                }
            }
        }

        public void AddClue(Clue clue)
        {
            if (clue == null)
                throw new ArgumentNullException(nameof(clue));
            _clues.Add(clue);
        }
    }
}
