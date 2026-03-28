using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Buses;

[ApiController]
[Route("api/buses")]
[Authorize]
public sealed class BusesController : ControllerBase
{
    private readonly BusRepository _repository;

    public BusesController(BusRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BusSummaryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _repository.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<object>> Create([FromBody] CreateBusRequest request, CancellationToken cancellationToken)
    {
        var busId = await _repository.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { busId }, new { busId });
    }
}
