using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.Parts;

public sealed class CreatePartRequest
{
    [Required]
    [MaxLength(50)]
    public string PartNumber { get; init; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }
    public string UnitCode { get; init; } = "EA";
    public decimal PurchasePriceExclTax { get; init; }
    public decimal SalePriceExclTax { get; init; }
    public decimal MinimumStock { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record PartSummaryDto(
    int PartId,
    string PartNumber,
    string Name,
    string UnitCode,
    decimal PurchasePriceExclTax,
    decimal SalePriceExclTax,
    decimal MinimumStock,
    decimal QuantityOnHand);
