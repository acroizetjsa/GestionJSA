using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Clients;

[ApiController]
[Route("api/clients")]
[Authorize]
public sealed class ClientsController : ControllerBase
{
    private readonly ClientRepository _repository;

    public ClientsController(ClientRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientSummaryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _repository.GetAllAsync(cancellationToken));

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<object>> CreateClient([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        var clientId = await _repository.CreateClientAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { clientId }, new { clientId });
    }

    [HttpGet("{clientId:int}/sites")]
    public async Task<ActionResult<IReadOnlyList<SiteSummaryDto>>> GetSites(int clientId, CancellationToken cancellationToken)
        => Ok(await _repository.GetSitesAsync(clientId, cancellationToken));

    [HttpPost("{clientId:int}/sites")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<object>> CreateSite(int clientId, [FromBody] CreateSiteRequest request, CancellationToken cancellationToken)
    {
        var siteId = await _repository.CreateSiteAsync(clientId, request, cancellationToken);
        return CreatedAtAction(nameof(GetSites), new { clientId }, new { siteId });
    }
}
