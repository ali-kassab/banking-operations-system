namespace Banking.Application.Exceptions;

/// <summary>Thrown when a requested record does not exist. Mapped to HTTP 404.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}
