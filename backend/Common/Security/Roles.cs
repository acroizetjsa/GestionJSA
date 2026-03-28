namespace Gmao.Api.Common.Security;

public static class Roles
{
    public const string Admin = "ADMIN";
    public const string Technician = "TECHNICIAN";

    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        Admin,
        Technician
    };

    public static bool IsAllowed(string? roleCode) => !string.IsNullOrWhiteSpace(roleCode) && AllowedRoles.Contains(roleCode);

    public static string Normalize(string roleCode) => roleCode.Trim().ToUpperInvariant();
}
