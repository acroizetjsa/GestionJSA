using Gmao.Api.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Auth;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly AuthRepository _repository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(AuthRepository repository, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await _repository.FindByUsernameAsync(username, cancellationToken);

        if (user is null || !user.IsActive || !_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            return Unauthorized(new { message = "Identifiants invalides." });
        }

        await _repository.UpdateLastLoginAsync(user.UserId, cancellationToken);
        var token = _jwtTokenService.CreateToken(user.UserId, user.Username, user.FullName, user.RoleCode);

        return Ok(new LoginResponse(token, user.UserId, user.Username, user.FullName, user.RoleCode));
    }
}
