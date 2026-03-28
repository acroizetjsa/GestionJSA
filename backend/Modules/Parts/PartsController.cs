using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Parts;

[ApiController]
[Route("api/parts")]
[Authorize]
public sealed class PartsController : ControllerBase
{
    private readonly PartRepository _repository;

    public PartsController(PartRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PartSummaryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _repository.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<object>> Create([FromBody] CreatePartRequest request, CancellationToken cancellationToken)
    {
        var partId = await _repository.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { partId }, new { partId });
    }
}
