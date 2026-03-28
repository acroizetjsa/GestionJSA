using System.ComponentModel.DataAnnotations;
using Gmao.Api.Common.Security;

namespace Gmao.Api.Modules.Users;

public sealed class CreateUserRequest
{
    [Required]
    [MaxLength(50)]
    public string Username { get; init; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string FullName { get; init; } = string.Empty;

    [EmailAddress]
    [MaxLength(120)]
    public string? Email { get; init; }

    [MaxLength(40)]
    public string? Phone { get; init; }

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    public string RoleCode { get; init; } = Roles.Technician;

    public bool IsActive { get; init; } = true;
}

public sealed record UserSummaryDto(
    int UserId,
    string Username,
    string FullName,
    string? Email,
    string? Phone,
    string RoleCode,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? LastLoginAtUtc);
