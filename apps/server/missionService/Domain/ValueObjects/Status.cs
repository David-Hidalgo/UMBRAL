using System;

namespace MissionService.Domain.ValueObjects
{
    public record Status
    {
        public int ActiveSessionCount { get; }

        public Status(int activeSessionCount = 0)
        {
            if (activeSessionCount < 0)
                throw new ArgumentException("Session count cannot be negative.", nameof(activeSessionCount));
            ActiveSessionCount = activeSessionCount;
        }

        public bool IsInUse => ActiveSessionCount > 0;

        public Status Increment() => new(ActiveSessionCount + 1);

        public Status Decrement()
        {
            if (ActiveSessionCount == 0)
                throw new InvalidOperationException("Session count is already zero.");
            return new(ActiveSessionCount - 1);
        }
    }
}
