using System;

namespace api.Application.Exceptions;

public sealed class MissionServiceException : Exception
{
    public int StatusCode { get; } = 502;
    public string? Content { get; }

    public MissionServiceException() : base("Upstream mission service error.")
    {
    }

    public MissionServiceException(string message) : base(message)
    {
    }

    public MissionServiceException(int statusCode, string? content, string message) : base(message)
    {
        StatusCode = statusCode;
        Content = content;
    }

    public MissionServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
