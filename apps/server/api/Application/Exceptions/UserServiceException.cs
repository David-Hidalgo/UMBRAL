using System;

namespace api.Application.Exceptions;

public sealed class UserServiceException : Exception
{
    public int StatusCode { get; }

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

    public UserServiceException(string message, Exception innerException) : base(message, innerException)
    {
        StatusCode = 502;
    }
}
