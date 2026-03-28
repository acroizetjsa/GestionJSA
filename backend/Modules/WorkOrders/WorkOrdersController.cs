using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.WorkOrders;

[ApiController]
[Route("api/work-orders")]
[Authorize]
public sealed class WorkOrdersController : ControllerBase
{
    private readonly WorkOrderRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public WorkOrdersController(WorkOrderRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkOrderSummaryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        return Ok(await _repository.GetAccessibleAsync(currentUser, cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<object>> Create([FromBody] CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        var workOrderId = await _repository.CreateAsync(request, currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { workOrderId }, new { workOrderId });
    }

    [HttpPost("{workOrderId:int}/close")]
    public async Task<IActionResult> Close(int workOrderId, [FromBody] CloseWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        await _repository.CloseAsync(workOrderId, request, currentUser, cancellationToken);
        return NoContent();
    }

    [HttpPost("{workOrderId:int}/consume-parts")]
    public async Task<IActionResult> ConsumePart(int workOrderId, [FromBody] ConsumePartRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        await _repository.ConsumePartAsync(workOrderId, request, currentUser, cancellationToken);
        return NoContent();
    }
}
