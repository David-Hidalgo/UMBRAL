using System;

namespace UserService.Domain.ValueObjects
{
    public record EmailAddress
    {
        public string Value { get; }

        private EmailAddress(string value) => Value = value;

        public static EmailAddress Create(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Email address cannot be empty.", nameof(value));
            var normalized = value.Trim().ToLowerInvariant();
            if (normalized.IndexOf('@') <= 0 || normalized.IndexOf('.', normalized.IndexOf('@')) < 0)
                throw new ArgumentException("Email address format is invalid.", nameof(value));
            return new EmailAddress(normalized);
        }

        public override string ToString() => Value;
    }
}
