using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.Buses;

public sealed class CreateBusRequest
{
    [Required]
    public int ClientId { get; init; }

    public int? SiteId { get; init; }

    [Required]
    [MaxLength(50)]
    public string FleetNumber { get; init; } = string.Empty;

    [MaxLength(30)]
    public string? RegistrationNumber { get; init; }

    [MaxLength(50)]
    public string? Vin { get; init; }

    [MaxLength(80)]
    public string? Brand { get; init; }

    [MaxLength(80)]
    public string? Model { get; init; }

    public int? YearOfManufacture { get; init; }
    public int CurrentMileageKm { get; init; }
    public string StatusCode { get; init; } = "ACTIVE";
    public string? Notes { get; init; }
}

public sealed record BusSummaryDto(
    int BusId,
    string FleetNumber,
    string? RegistrationNumber,
    string? Vin,
    string? Brand,
    string? Model,
    int? YearOfManufacture,
    int CurrentMileageKm,
    string StatusCode,
    string ClientName,
    string? SiteName);
