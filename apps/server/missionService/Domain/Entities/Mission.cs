using System;
using System.Collections.Generic;
using System.Linq;
using MissionService.Domain.Exceptions;
using MissionService.Domain.ValueObjects;

namespace MissionService.Domain.Entities
{
    public class Mission
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public MissionStatus Status { get; private set; } = MissionStatus.Inactive;

        private readonly List<Stage> _stages = new();
        public IReadOnlyCollection<Stage> Stages => _stages.AsReadOnly();

        private Mission() { }

        public static Mission Create(Guid id, string name, string description, List<Stage> stages)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Mission ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Mission name cannot be empty.", nameof(name));

            var trimmedName = name.Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
                throw new ArgumentException("Mission name cannot be empty or consist only of whitespace.", nameof(name));

            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Mission description cannot be empty.", nameof(description));

            var mission = new Mission
            {
                Id = id,
                Name = trimmedName,
                Description = description,
                Status = MissionStatus.Inactive
            };

            if (stages != null)
            {
                foreach (var stage in stages)
                {
                    mission.AddStage(stage);
                }
            }

            return mission;
        }

        public void Activate()
        {
            Status = MissionStatus.Active;
        }

        public void Deactivate()
        {
            Status = MissionStatus.Inactive;
        }

        public void EnsureCanModifyStages()
        {
            if (!Status.CanModifyStages())
            {
                throw new MissionActiveException(Id);
            }
        }

        public void AddStage(Stage stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            EnsureCanModifyStages();

            // Validate uniqueness of stage name and order
            if (_stages.Any(s => s.Name.Equals(stage.Name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"A stage with the name '{stage.Name}' already exists in this mission.");
            if (_stages.Any(s => s.Order == stage.Order))
                throw new ArgumentException($"A stage with the order '{stage.Order}' already exists in this mission.");

            _stages.Add(stage);
        }
    }
}
