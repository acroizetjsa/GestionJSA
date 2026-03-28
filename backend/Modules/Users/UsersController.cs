using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Users;

[ApiController]
[Route("api/users")]
[Authorize(Roles = Roles.Admin)]
public sealed class UsersController : ControllerBase
{
    private readonly UserRepository _repository;
    private readonly IPasswordHasher _passwordHasher;

    public UsersController(UserRepository repository, IPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserSummaryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _repository.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!Roles.IsAllowed(request.RoleCode))
        {
            return BadRequest(new { message = "RoleCode invalide." });
        }

        var (hash, salt) = _passwordHasher.CreateHash(request.Password);
        var normalizedRequest = new CreateUserRequest
        {
            Username = request.Username,
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Password = request.Password,
            RoleCode = Roles.Normalize(request.RoleCode),
            IsActive = request.IsActive
        };
        var userId = await _repository.CreateAsync(normalizedRequest, hash, salt, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { userId }, new { userId });
    }
}
