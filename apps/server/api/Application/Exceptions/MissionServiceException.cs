using System;

namespace api.Application.Exceptions;

public sealed class MissionServiceException : Exception
{
    public MissionServiceException() : base("Upstream mission service error.")
    {
    }

    public MissionServiceException(string message) : base(message)
    {
    }

    public MissionServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
