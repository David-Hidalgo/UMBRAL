using System;
using System.Collections.Generic;
using System.Linq;
using MissionService.Domain.Enums;
using MissionService.Domain.Exceptions;
using MissionService.Domain.ValueObjects;

namespace MissionService.Domain.Entities
{
    public class Mission
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public MissionState State { get; private set; } = MissionState.Inactive;
        public Status Status { get; private set; } = new Status(0);
        public DifficultyLevel DifficultyLevel { get; private set; }
        public int MaxTimeMinutes { get; private set; }

        private readonly List<Stage> _stages = new();
        public IReadOnlyCollection<Stage> Stages => _stages.AsReadOnly();

        private Mission() { }

        public static Mission Create(Guid id, string name, string description, DifficultyLevel difficultyLevel, int maxTimeMinutes, List<Stage> stages)
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
            if (maxTimeMinutes <= 0)
                throw new ArgumentException("Max time minutes must be greater than zero.", nameof(maxTimeMinutes));

            var mission = new Mission
            {
                Id = id,
                Name = trimmedName,
                Description = description,
                State = MissionState.Inactive,
                Status = new Status(0),
                DifficultyLevel = difficultyLevel,
                MaxTimeMinutes = maxTimeMinutes
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

        public void UpdateInfo(string? name, string? description, DifficultyLevel? difficultyLevel, int? maxTimeMinutes)
        {
            EnsureCanModify();

            if (name != null)
            {
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("Mission name cannot be empty.", nameof(name));
                var trimmedName = name.Trim();
                if (string.IsNullOrWhiteSpace(trimmedName))
                    throw new ArgumentException("Mission name cannot be empty or consist only of whitespace.", nameof(name));
                Name = trimmedName;
            }

            if (description != null)
            {
                if (string.IsNullOrWhiteSpace(description))
                    throw new ArgumentException("Mission description cannot be empty.", nameof(description));
                Description = description;
            }

            if (difficultyLevel.HasValue)
            {
                DifficultyLevel = difficultyLevel.Value;
            }

            if (maxTimeMinutes.HasValue)
            {
                if (maxTimeMinutes.Value <= 0)
                    throw new ArgumentException("Max time minutes must be greater than zero.", nameof(maxTimeMinutes));
                MaxTimeMinutes = maxTimeMinutes.Value;
            }
        }

        public void Activate()
        {
            State = MissionState.Active;
        }

        public void Deactivate()
        {
            if (Status.IsInUse)
                throw new MissionHasActiveSessionsException(Id);

            if (_stages.Any(s => s.Status.IsInUse))
                throw new MissionHasActiveSessionsException(Id);

            State = MissionState.Inactive;
        }

        public void IncrementSessionCount() => Status = Status.Increment();
        public void DecrementSessionCount() => Status = Status.Decrement();

        public void EnsureCanModify()
        {
            if (Status.IsInUse)
            {
                throw new MissionActiveException(Id);
            }
        }

        public void AddStage(Stage stage)
        {
            if (stage == null)
                throw new ArgumentNullException(nameof(stage));

            EnsureCanModify();

            // Validate uniqueness of stage name and order
            if (_stages.Any(s => s.Name.Equals(stage.Name, StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException($"A stage with the name '{stage.Name}' already exists in this mission.");
            if (_stages.Any(s => s.Order == stage.Order))
                throw new ArgumentException($"A stage with the order '{stage.Order}' already exists in this mission.");

            _stages.Add(stage);
        }

        public void RemoveStage(Guid stageId)
        {
            EnsureCanModify();

            var stage = _stages.FirstOrDefault(s => s.Id == stageId);
            if (stage == null)
                throw new StageNotFoundException(stageId);

            stage.EnsureCanModify();

            if (_stages.Count <= 1)
                throw new CannotRemoveLastStageException(Id);

            _stages.Remove(stage);
        }

        public void ReorderStages(List<Guid> stageIdsInOrder)
        {
            EnsureCanModify();

            if (stageIdsInOrder == null || stageIdsInOrder.Count != _stages.Count)
                throw new ArgumentException("Provided stage IDs do not match the stages of this mission.", nameof(stageIdsInOrder));

            // Verify all IDs belong to this mission
            foreach (var id in stageIdsInOrder)
            {
                if (!_stages.Any(s => s.Id == id))
                    throw new ArgumentException($"Stage with ID '{id}' does not belong to this mission.", nameof(stageIdsInOrder));
            }

            // Reorder
            for (int i = 0; i < stageIdsInOrder.Count; i++)
            {
                var id = stageIdsInOrder[i];
                var stage = _stages.First(s => s.Id == id);
                stage.SetOrder(i + 1); // 1-based ordering
            }
        }

        public void UpdateStage(Guid stageId, string name, string description, List<SubStage> newSubStages)
        {
            var stage = _stages.FirstOrDefault(s => s.Id == stageId);
            if (stage == null)
                throw new StageNotFoundException(stageId);

            stage.Update(name, description, newSubStages);
        }
    }
}
