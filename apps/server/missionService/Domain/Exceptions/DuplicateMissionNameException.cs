namespace MissionService.Domain.Exceptions
{
    public sealed class DuplicateMissionNameException : DomainException
    {
        public DuplicateMissionNameException(string name) 
            : base($"A mission with the name '{name}' already exists.") { }
    }
}
