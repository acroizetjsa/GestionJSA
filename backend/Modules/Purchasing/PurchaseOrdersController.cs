using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Purchasing;

[ApiController]
[Route("api/purchase-orders")]
[Authorize(Roles = Roles.Admin)]
public sealed class PurchaseOrdersController : ControllerBase
{
    private readonly PurchaseOrderRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public PurchaseOrdersController(PurchaseOrderRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PurchaseOrderSummaryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _repository.GetAllAsync(cancellationToken));

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        var purchaseOrderId = await _repository.CreateAsync(request, currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { purchaseOrderId }, new { purchaseOrderId });
    }

    [HttpPost("{purchaseOrderId:int}/receive")]
    public async Task<IActionResult> Receive(int purchaseOrderId, [FromBody] ReceivePurchaseOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        await _repository.ReceiveAsync(purchaseOrderId, request, currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
