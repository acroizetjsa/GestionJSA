using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.Auth;

public sealed class LoginRequest
{
    [Required]
    public string Username { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record LoginResponse(string Token, int UserId, string Username, string FullName, string RoleCode);

public sealed record AuthUserRecord(int UserId, string Username, string PasswordHash, string PasswordSalt, string FullName, string RoleCode, bool IsActive);
