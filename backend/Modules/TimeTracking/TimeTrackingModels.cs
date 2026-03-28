using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.TimeTracking;

public sealed class StartTimeEntryRequest
{
    public int? WorkOrderId { get; init; }
    public decimal? StartLatitude { get; init; }
    public decimal? StartLongitude { get; init; }
    public decimal? StartAccuracyMeters { get; init; }
    public string? Notes { get; init; }
}

public sealed class StopTimeEntryRequest
{
    public decimal? EndLatitude { get; init; }
    public decimal? EndLongitude { get; init; }
    public decimal? EndAccuracyMeters { get; init; }
}

public sealed record TimeEntryDto(
    int TimeEntryId,
    int TechnicianId,
    int? WorkOrderId,
    DateTime StartAtUtc,
    DateTime? EndAtUtc,
    string StatusCode,
    decimal? StartLatitude,
    decimal? StartLongitude,
    decimal? EndLatitude,
    decimal? EndLongitude,
    string? Notes);
