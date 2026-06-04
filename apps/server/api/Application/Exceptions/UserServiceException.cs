using System;

namespace api.Application.Exceptions;

public sealed class UserServiceException : Exception
{
    public int StatusCode { get; } = 502;
    public string? Content { get; }

    public UserServiceException() : base("Upstream user service error.")
    {
        StatusCode = 502;
    }

    public UserServiceException(string message) : base(message)
    {
        StatusCode = 502;
    }

    public UserServiceException(int statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public UserServiceException(int statusCode, string? content, string message) : base(message)
    {
        StatusCode = statusCode;
        Content = content;
    }

    public UserServiceException(string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = 502;
    }
}
