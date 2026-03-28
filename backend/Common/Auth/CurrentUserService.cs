using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Gmao.Api.Common.Auth;

public sealed record CurrentUser(int UserId, string Username, string FullName, string RoleCode);

public interface ICurrentUserService
{
    CurrentUser GetRequired();
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public CurrentUser GetRequired()
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("Utilisateur non authentifié.");
        }

        var userIdRaw = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Claim userId manquant.");
        var username = principal.FindFirstValue(ClaimTypes.Name) ?? throw new UnauthorizedAccessException("Claim username manquant.");
        var fullName = principal.FindFirstValue("full_name") ?? username;
        var roleCode = principal.FindFirstValue(ClaimTypes.Role) ?? throw new UnauthorizedAccessException("Claim role manquant.");

        return new CurrentUser(int.Parse(userIdRaw), username, fullName, roleCode);
    }
}
