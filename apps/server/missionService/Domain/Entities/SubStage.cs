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

        protected void AddClue(Clue clue)
        {
            if (clue == null)
                throw new ArgumentNullException(nameof(clue));
            _clues.Add(clue);
        }
    }
}
