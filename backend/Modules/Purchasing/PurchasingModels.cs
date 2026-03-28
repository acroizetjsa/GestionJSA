using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.Purchasing;

public sealed class CreatePurchaseOrderLineRequest
{
    [Required]
    public int PartId { get; init; }

    [Range(typeof(decimal), "0.001", "999999")]
    public decimal QuantityOrdered { get; init; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal UnitPriceExclTax { get; init; }

    public decimal TaxRate { get; init; } = 20;
}

public sealed class CreatePurchaseOrderRequest
{
    [Required]
    public int SupplierId { get; init; }

    public int? SiteId { get; init; }
    public DateTime? ExpectedAtUtc { get; init; }
    public string? Notes { get; init; }
    public List<CreatePurchaseOrderLineRequest> Lines { get; init; } = [];
}

public sealed class ReceivePurchaseOrderLineRequest
{
    [Required]
    public int PurchaseOrderLineId { get; init; }

    [Required]
    public int WarehouseId { get; init; }

    [Range(typeof(decimal), "0.001", "999999")]
    public decimal QuantityReceived { get; init; }
}

public sealed class ReceivePurchaseOrderRequest
{
    public List<ReceivePurchaseOrderLineRequest> Lines { get; init; } = [];
}

public sealed record PurchaseOrderSummaryDto(
    int PurchaseOrderId,
    string PurchaseOrderNumber,
    string SupplierName,
    string? SiteName,
    string StatusCode,
    DateTime OrderedAtUtc,
    DateTime? ExpectedAtUtc,
    decimal TotalExclTax,
    decimal TotalInclTax);
