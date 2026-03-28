using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.Invoicing;

[ApiController]
[Route("api/invoices")]
[Authorize(Roles = Roles.Admin)]
public sealed class InvoicesController : ControllerBase
{
    private readonly InvoicingRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public InvoicesController(InvoicingRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<InvoiceSummaryDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _repository.GetAllAsync(cancellationToken));

    [HttpPost("from-work-order")]
    public async Task<ActionResult<object>> CreateFromWorkOrder([FromBody] CreateInvoiceFromWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        var invoiceId = await _repository.CreateFromWorkOrderAsync(request, currentUser.UserId, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { invoiceId }, new { invoiceId });
    }

    [HttpPost("{invoiceId:int}/issue")]
    public async Task<IActionResult> Issue(int invoiceId, CancellationToken cancellationToken)
    {
        await _repository.IssueAsync(invoiceId, cancellationToken);
        return NoContent();
    }

    [HttpPost("payments")]
    public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        await _repository.RecordPaymentAsync(request, currentUser.UserId, cancellationToken);
        return NoContent();
    }
}
