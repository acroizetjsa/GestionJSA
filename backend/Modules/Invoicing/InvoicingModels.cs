using System.ComponentModel.DataAnnotations;

namespace Gmao.Api.Modules.Invoicing;

public sealed class CreateInvoiceFromWorkOrderRequest
{
    [Required]
    public int WorkOrderId { get; init; }

    [Range(typeof(decimal), "0", "999999")]
    public decimal HourlyRateExclTax { get; init; }

    public decimal TaxRate { get; init; } = 20;
    public DateTime? DueDateUtc { get; init; }
    public string? Notes { get; init; }
}

public sealed class RecordPaymentRequest
{
    [Required]
    public int InvoiceId { get; init; }

    [Range(typeof(decimal), "0.01", "999999")]
    public decimal Amount { get; init; }

    public DateTime? PaymentDateUtc { get; init; }
    public string MethodCode { get; init; } = "TRANSFER";
    public string? Reference { get; init; }
    public string? Notes { get; init; }
}

public sealed record InvoiceSummaryDto(
    int InvoiceId,
    string InvoiceNumber,
    string ClientName,
    string? SiteName,
    string StatusCode,
    DateTime IssueDate,
    DateTime DueDate,
    decimal TotalInclTax,
    decimal PaidAmount,
    decimal Balance);
