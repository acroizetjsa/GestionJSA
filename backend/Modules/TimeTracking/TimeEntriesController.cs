using Gmao.Api.Common.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gmao.Api.Modules.TimeTracking;

[ApiController]
[Route("api/time-entries")]
[Authorize]
public sealed class TimeEntriesController : ControllerBase
{
    private readonly TimeTrackingRepository _repository;
    private readonly ICurrentUserService _currentUserService;

    public TimeEntriesController(TimeTrackingRepository repository, ICurrentUserService currentUserService)
    {
        _repository = repository;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TimeEntryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        return Ok(await _repository.GetAccessibleAsync(currentUser, cancellationToken));
    }

    [HttpPost("start")]
    public async Task<ActionResult<object>> Start([FromBody] StartTimeEntryRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        var timeEntryId = await _repository.StartAsync(request, currentUser, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { timeEntryId }, new { timeEntryId });
    }

    [HttpPost("{timeEntryId:int}/stop")]
    public async Task<IActionResult> Stop(int timeEntryId, [FromBody] StopTimeEntryRequest request, CancellationToken cancellationToken)
    {
        var currentUser = _currentUserService.GetRequired();
        await _repository.StopAsync(timeEntryId, request, currentUser, cancellationToken);
        return NoContent();
    }
}
