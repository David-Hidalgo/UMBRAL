using System;

namespace UserService.Domain.ValueObjects
{
    public record FullName
    {
        public string Value { get; }

        private FullName(string value) => Value = value;

        public static FullName Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Full name cannot be empty.", nameof(value));
            if (value.Trim().Length > 200)
                throw new ArgumentException("Full name cannot exceed 200 characters.", nameof(value));
            return new FullName(value.Trim());
        }

        public override string ToString() => Value;
    }
}
