using System;

namespace api.Application.Exceptions;

public sealed class UserServiceException : Exception
{
    public UserServiceException() : base("Upstream user service error.")
    {
    }

    public UserServiceException(string message) : base(message)
    {
    }

    public UserServiceException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
