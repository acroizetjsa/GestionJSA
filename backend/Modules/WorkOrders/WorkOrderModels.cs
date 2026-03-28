using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.WorkOrders;

public sealed class CreateWorkOrderRequest
{
    [Required]
    public string TypeCode { get; init; } = "CURATIVE";

    [Required]
    public int ClientId { get; init; }

    public int? SiteId { get; init; }
    public int? BusId { get; init; }
    public int? EquipmentId { get; init; }

    [Required]
    [MaxLength(150)]
    public string Title { get; init; } = string.Empty;

    public string? Description { get; init; }
    public string PriorityCode { get; init; } = "NORMAL";
    public DateTime? ScheduledStartUtc { get; init; }
    public DateTime? ScheduledEndUtc { get; init; }
    public int? AssignedTechnicianId { get; init; }
}

public sealed class CloseWorkOrderRequest
{
    public string? ResolutionNotes { get; init; }
    public int LaborMinutes { get; init; }
    public decimal TravelKm { get; init; }
}

public sealed class ConsumePartRequest
{
    [Required]
    public int PartId { get; init; }

    [Required]
    public int WarehouseId { get; init; }

    [Range(typeof(decimal), "0.001", "999999")]
    public decimal Quantity { get; init; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal UnitSalePrice { get; init; }
}

public sealed record WorkOrderSummaryDto(
    int WorkOrderId,
    string WorkOrderNumber,
    string TypeCode,
    string StatusCode,
    string PriorityCode,
    string ClientName,
    string? SiteName,
    string? BusLabel,
    string Title,
    string? AssignedTechnician,
    DateTime ReportedAtUtc,
    DateTime? ScheduledStartUtc,
    DateTime? ClosedAtUtc);
