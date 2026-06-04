using System;

namespace MissionService.Domain.ValueObjects
{
    public record MissionStatus
    {
        public static readonly MissionStatus Inactive = new("Inactive");
        public static readonly MissionStatus Active = new("Active");

        public string Value { get; }

        private MissionStatus(string value)
        {
            Value = value;
        }

        public bool CanModifyStages() => this == Inactive;

        public static MissionStatus FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Status value cannot be empty.", nameof(value));

            var normalized = value.Trim();
            if (normalized.Equals(Inactive.Value, StringComparison.OrdinalIgnoreCase))
                return Inactive;
            if (normalized.Equals(Active.Value, StringComparison.OrdinalIgnoreCase))
                return Active;

            throw new ArgumentException($"Invalid status value: '{value}'", nameof(value));
        }

        public override string ToString() => Value;
    }
}
