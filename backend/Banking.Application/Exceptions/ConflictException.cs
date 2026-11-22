namespace Banking.Application.Exceptions;

/// <summary>Thrown when data clashes with existing data (e.g. a duplicate email). Mapped to HTTP 409.</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
