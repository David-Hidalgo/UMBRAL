using System;
using System.Collections.Generic;
using MissionService.Domain.Exceptions;
using MissionService.Domain.ValueObjects;

namespace MissionService.Domain.Entities
{
    public class Stage
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public int Order { get; private set; }
        public Guid MissionId { get; private set; }
        public Status Status { get; private set; } = new Status(0);

        private readonly List<SubStage> _subStages = new();
        public IReadOnlyCollection<SubStage> SubStages => _subStages.AsReadOnly();

        private Stage() { }

        public static Stage Create(Guid id, string name, string description, int order, List<SubStage> subStages)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Stage ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Stage name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Stage description cannot be empty.", nameof(description));
            if (order < 0)
                throw new ArgumentException("Stage order cannot be negative.", nameof(order));

            var stage = new Stage
            {
                Id = id,
                Name = name,
                Description = description,
                Order = order
            };

            if (subStages != null)
            {
                foreach (var ss in subStages)
                {
                    stage.AddSubStage(ss);
                }
            }

            return stage;
        }

        public void Update(string name, string description, List<SubStage> newSubStages)
        {
            EnsureCanModify();

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Stage name cannot be empty.", nameof(name));
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Stage description cannot be empty.", nameof(description));

            Name = name;
            Description = description;

            _subStages.Clear();
            if (newSubStages != null)
            {
                foreach (var ss in newSubStages)
                {
                    AddSubStage(ss);
                }
            }
        }

        public void SetOrder(int order)
        {
            if (order < 0)
                throw new ArgumentException("Stage order cannot be negative.", nameof(order));
            Order = order;
        }

        public void IncrementSessionCount() => Status = Status.Increment();
        public void DecrementSessionCount() => Status = Status.Decrement();

        public void EnsureCanModify()
        {
            if (Status.IsInUse)
            {
                throw new StageActiveException(Id);
            }
        }

        private void AddSubStage(SubStage subStage)
        {
            if (subStage == null)
                throw new ArgumentNullException(nameof(subStage));
            _subStages.Add(subStage);
        }
    }
}
