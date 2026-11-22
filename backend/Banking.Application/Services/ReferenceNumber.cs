namespace Banking.Application.Services;

public static class ReferenceNumber
{
    /// <summary>Creates a human-readable unique reference such as "TXN-20261122-3F9A1C7B".</summary>
    public static string New() =>
        $"TXN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
