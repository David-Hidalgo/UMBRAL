using System;

namespace MissionService.Domain.Entities
{
    public abstract class SubStage
    {
        public Guid Id { get; protected set; }
        public string Title { get; protected set; } = string.Empty;
        public int Order { get; protected set; }
        public int Points { get; protected set; }
        public Guid StageId { get; protected set; }

        protected SubStage() { }
    }
}
