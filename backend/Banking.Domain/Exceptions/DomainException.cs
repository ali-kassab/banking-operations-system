namespace Banking.Domain.Exceptions;

/// <summary>
/// Thrown when an operation would break a business rule
/// (for example: withdrawing more money than the account holds).
/// The API turns this into an HTTP 400 response.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
