namespace Banking.API.Authorization;

/// <summary>Role names used in [Authorize(Roles = ...)] attributes. They match the UserRole enum.</summary>
public static class Roles
{
    public const string Admin = nameof(Banking.Domain.Enums.UserRole.Admin);
    public const string Employee = nameof(Banking.Domain.Enums.UserRole.Employee);
}
