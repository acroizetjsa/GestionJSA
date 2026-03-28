using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.Clients;

public sealed class CreateClientRequest
{
    [Required]
    [MaxLength(30)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    public string? BillingAddressLine1 { get; init; }
    public string? BillingAddressLine2 { get; init; }
    public string? BillingPostalCode { get; init; }
    public string? BillingCity { get; init; }
    public string? BillingCountry { get; init; }
    public string? VatNumber { get; init; }
    public int PaymentTermDays { get; init; } = 30;
    public decimal DefaultHourlyRateExclTax { get; init; } = 75;
    public bool IsActive { get; init; } = true;
}

public sealed class CreateSiteRequest
{
    [Required]
    [MaxLength(30)]
    public string Code { get; init; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; init; } = string.Empty;

    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? PostalCode { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public bool IsActive { get; init; } = true;
}

public sealed record ClientSummaryDto(int ClientId, string Code, string Name, decimal DefaultHourlyRateExclTax, int PaymentTermDays, bool IsActive, int ActiveSiteCount);
public sealed record SiteSummaryDto(int SiteId, int ClientId, string Code, string Name, string? City, string? Country, string? ContactName, string? ContactPhone, bool IsActive);
