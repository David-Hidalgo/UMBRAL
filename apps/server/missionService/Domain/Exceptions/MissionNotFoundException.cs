namespace MissionService.Domain.Exceptions
{
    public sealed class MissionNotFoundException : DomainException
    {
        public MissionNotFoundException(Guid id)
            : base($"Mission with ID '{id}' was not found.") { }
    }
}
