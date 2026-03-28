using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Invoicing;

public sealed class InvoicingRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public InvoicingRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<InvoiceSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT i.InvoiceId, i.InvoiceNumber, c.Name AS ClientName, s.Name AS SiteName, i.StatusCode,
                   CAST(i.IssueDate AS DATETIME2) AS IssueDate, CAST(i.DueDate AS DATETIME2) AS DueDate,
                   v.TotalInclTax, v.PaidAmount, v.Balance
            FROM bill.Invoices i
            INNER JOIN crm.Clients c ON c.ClientId = i.ClientId
            LEFT JOIN crm.Sites s ON s.SiteId = i.SiteId
            INNER JOIN bill.vInvoiceBalance v ON v.InvoiceId = i.InvoiceId
            ORDER BY i.IssueDate DESC, i.InvoiceNumber DESC;
            """;

        var results = new List<InvoiceSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new InvoiceSummaryDto(
                reader.GetRequiredInt32("InvoiceId"),
                reader.GetRequiredString("InvoiceNumber"),
                reader.GetRequiredString("ClientName"),
                reader.GetNullableString("SiteName"),
                reader.GetRequiredString("StatusCode"),
                reader.GetRequiredDateTime("IssueDate"),
                reader.GetRequiredDateTime("DueDate"),
                reader.GetRequiredDecimal("TotalInclTax"),
                reader.GetRequiredDecimal("PaidAmount"),
                reader.GetRequiredDecimal("Balance")));
        }

        return results;
    }

    public async Task<int> CreateFromWorkOrderAsync(CreateInvoiceFromWorkOrderRequest request, int createdByUserId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string workOrderSql = """
                SELECT wo.WorkOrderId, wo.WorkOrderNumber, wo.ClientId, wo.SiteId, wo.LaborMinutes, c.PaymentTermDays
                FROM ops.WorkOrders wo
                INNER JOIN crm.Clients c ON c.ClientId = wo.ClientId
                WHERE wo.WorkOrderId = @WorkOrderId;
                """;

            int clientId;
            int? siteId;
            int laborMinutes;
            int paymentTermDays;
            string workOrderNumber;

            await using (var workOrderCommand = new SqlCommand(workOrderSql, connection, (SqlTransaction)transaction))
            {
                workOrderCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = request.WorkOrderId;
                await using var reader = await workOrderCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new KeyNotFoundException("Ordre de travail introuvable.");
                }

                workOrderNumber = reader.GetRequiredString("WorkOrderNumber");
                clientId = reader.GetRequiredInt32("ClientId");
                siteId = reader.GetNullableInt32("SiteId");
                laborMinutes = reader.GetRequiredInt32("LaborMinutes");
                paymentTermDays = reader.GetRequiredInt32("PaymentTermDays");
            }

            const string existingInvoiceSql = "SELECT COUNT(1) FROM bill.InvoiceWorkOrders WHERE WorkOrderId = @WorkOrderId;";
            await using (var existingCommand = new SqlCommand(existingInvoiceSql, connection, (SqlTransaction)transaction))
            {
                existingCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = request.WorkOrderId;
                var alreadyLinked = Convert.ToInt32(await existingCommand.ExecuteScalarAsync(cancellationToken));
                if (alreadyLinked > 0)
                {
                    throw new InvalidOperationException("Une facture existe déjà pour cet ordre de travail.");
                }
            }

            var issueDate = DateTime.UtcNow.Date;
            var dueDate = request.DueDateUtc?.Date ?? issueDate.AddDays(paymentTermDays);

            const string insertInvoiceSql = """
                DECLARE @Sequence INT = NEXT VALUE FOR bill.InvoiceNumberSeq;
                DECLARE @InvoiceNumber NVARCHAR(30) = CONCAT('FAC-', YEAR(SYSUTCDATETIME()), '-', RIGHT(REPLICATE('0', 6) + CAST(@Sequence AS VARCHAR(6)), 6));

                INSERT INTO bill.Invoices
                (
                    InvoiceNumber, ClientId, SiteId, StatusCode, IssueDate, DueDate, Notes, CreatedByUserId
                )
                VALUES
                (
                    @InvoiceNumber, @ClientId, @SiteId, 'DRAFT', @IssueDate, @DueDate, @Notes, @CreatedByUserId
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            int invoiceId;
            await using (var invoiceCommand = new SqlCommand(insertInvoiceSql, connection, (SqlTransaction)transaction))
            {
                invoiceCommand.Parameters.Add("@ClientId", System.Data.SqlDbType.Int).Value = clientId;
                invoiceCommand.Parameters.Add("@SiteId", System.Data.SqlDbType.Int).Value = (object?)siteId ?? DBNull.Value;
                invoiceCommand.Parameters.Add("@IssueDate", System.Data.SqlDbType.Date).Value = issueDate;
                invoiceCommand.Parameters.Add("@DueDate", System.Data.SqlDbType.Date).Value = dueDate;
                invoiceCommand.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 2000).Value = (object?)request.Notes ?? DBNull.Value;
                invoiceCommand.Parameters.Add("@CreatedByUserId", System.Data.SqlDbType.Int).Value = createdByUserId;
                invoiceId = Convert.ToInt32(await invoiceCommand.ExecuteScalarAsync(cancellationToken));
            }

            const string insertLineSql = """
                INSERT INTO bill.InvoiceLines (InvoiceId, Description, Quantity, UnitPriceExclTax, TaxRate, SortOrder)
                VALUES (@InvoiceId, @Description, @Quantity, @UnitPriceExclTax, @TaxRate, @SortOrder);
                """;

            if (laborMinutes > 0 && request.HourlyRateExclTax > 0)
            {
                await using var laborCommand = new SqlCommand(insertLineSql, connection, (SqlTransaction)transaction);
                laborCommand.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = invoiceId;
                laborCommand.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 250).Value = $"Main d'oeuvre {workOrderNumber}";
                laborCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Decimal).Value = Math.Round(laborMinutes / 60m, 2);
                laborCommand.Parameters.Add("@UnitPriceExclTax", System.Data.SqlDbType.Decimal).Value = request.HourlyRateExclTax;
                laborCommand.Parameters.Add("@TaxRate", System.Data.SqlDbType.Decimal).Value = request.TaxRate;
                laborCommand.Parameters.Add("@SortOrder", System.Data.SqlDbType.Int).Value = 1;
                await laborCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string partSql = """
                SELECT p.Name, wp.Quantity, wp.UnitSalePrice
                FROM ops.WorkOrderParts wp
                INNER JOIN stock.Parts p ON p.PartId = wp.PartId
                WHERE wp.WorkOrderId = @WorkOrderId
                ORDER BY wp.WorkOrderPartId;
                """;

            var sortOrder = 10;
            await using (var partCommand = new SqlCommand(partSql, connection, (SqlTransaction)transaction))
            {
                partCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = request.WorkOrderId;
                await using var reader = await partCommand.ExecuteReaderAsync(cancellationToken);
                var partLines = new List<(string Name, decimal Quantity, decimal UnitSalePrice)>();
                while (await reader.ReadAsync(cancellationToken))
                {
                    partLines.Add((
                        reader.GetRequiredString("Name"),
                        reader.GetRequiredDecimal("Quantity"),
                        reader.GetRequiredDecimal("UnitSalePrice")));
                }

                await reader.CloseAsync();

                foreach (var partLine in partLines)
                {
                    await using var insertPartCommand = new SqlCommand(insertLineSql, connection, (SqlTransaction)transaction);
                    insertPartCommand.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = invoiceId;
                    insertPartCommand.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 250).Value = $"Pièce: {partLine.Name}";
                    insertPartCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Decimal).Value = partLine.Quantity;
                    insertPartCommand.Parameters.Add("@UnitPriceExclTax", System.Data.SqlDbType.Decimal).Value = partLine.UnitSalePrice;
                    insertPartCommand.Parameters.Add("@TaxRate", System.Data.SqlDbType.Decimal).Value = request.TaxRate;
                    insertPartCommand.Parameters.Add("@SortOrder", System.Data.SqlDbType.Int).Value = sortOrder;
                    await insertPartCommand.ExecuteNonQueryAsync(cancellationToken);
                    sortOrder += 10;
                }
            }

            const string linkSql = """
                INSERT INTO bill.InvoiceWorkOrders (InvoiceId, WorkOrderId)
                VALUES (@InvoiceId, @WorkOrderId);
                """;

            await using (var linkCommand = new SqlCommand(linkSql, connection, (SqlTransaction)transaction))
            {
                linkCommand.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = invoiceId;
                linkCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = request.WorkOrderId;
                await linkCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return invoiceId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task IssueAsync(int invoiceId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE bill.Invoices
            SET StatusCode = 'ISSUED'
            WHERE InvoiceId = @InvoiceId
              AND StatusCode = 'DRAFT';
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = invoiceId;
        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
        {
            throw new InvalidOperationException("La facture ne peut pas être émise dans son état actuel.");
        }
    }

    public async Task RecordPaymentAsync(RecordPaymentRequest request, int userId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string invoiceSql = """
                SELECT i.ClientId, v.Balance, v.TotalInclTax
                FROM bill.Invoices i
                INNER JOIN bill.vInvoiceBalance v ON v.InvoiceId = i.InvoiceId
                WHERE i.InvoiceId = @InvoiceId;
                """;

            int clientId;
            decimal balance;
            decimal totalInclTax;
            await using (var invoiceCommand = new SqlCommand(invoiceSql, connection, (SqlTransaction)transaction))
            {
                invoiceCommand.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = request.InvoiceId;
                await using var reader = await invoiceCommand.ExecuteReaderAsync(cancellationToken);
                if (!await reader.ReadAsync(cancellationToken))
                {
                    throw new KeyNotFoundException("Facture introuvable.");
                }

                clientId = reader.GetRequiredInt32("ClientId");
                balance = reader.GetRequiredDecimal("Balance");
                totalInclTax = reader.GetRequiredDecimal("TotalInclTax");
            }

            if (request.Amount > balance)
            {
                throw new InvalidOperationException("Le montant pointé dépasse le solde restant dû.");
            }

            const string paymentSql = """
                INSERT INTO bill.Payments
                (
                    ClientId, PaymentDate, Amount, MethodCode, Reference, Notes, ReceivedByUserId
                )
                VALUES
                (
                    @ClientId, @PaymentDate, @Amount, @MethodCode, @Reference, @Notes, @ReceivedByUserId
                );
                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            int paymentId;
            await using (var paymentCommand = new SqlCommand(paymentSql, connection, (SqlTransaction)transaction))
            {
                paymentCommand.Parameters.Add("@ClientId", System.Data.SqlDbType.Int).Value = clientId;
                paymentCommand.Parameters.Add("@PaymentDate", System.Data.SqlDbType.Date).Value = request.PaymentDateUtc?.Date ?? DateTime.UtcNow.Date;
                paymentCommand.Parameters.Add("@Amount", System.Data.SqlDbType.Decimal).Value = request.Amount;
                paymentCommand.Parameters.Add("@MethodCode", System.Data.SqlDbType.NVarChar, 20).Value = request.MethodCode.Trim().ToUpperInvariant();
                paymentCommand.Parameters.Add("@Reference", System.Data.SqlDbType.NVarChar, 100).Value = (object?)request.Reference ?? DBNull.Value;
                paymentCommand.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 1000).Value = (object?)request.Notes ?? DBNull.Value;
                paymentCommand.Parameters.Add("@ReceivedByUserId", System.Data.SqlDbType.Int).Value = userId;
                paymentId = Convert.ToInt32(await paymentCommand.ExecuteScalarAsync(cancellationToken));
            }

            const string allocationSql = """
                INSERT INTO bill.PaymentAllocations (PaymentId, InvoiceId, AmountAllocated)
                VALUES (@PaymentId, @InvoiceId, @AmountAllocated);
                """;

            await using (var allocationCommand = new SqlCommand(allocationSql, connection, (SqlTransaction)transaction))
            {
                allocationCommand.Parameters.Add("@PaymentId", System.Data.SqlDbType.Int).Value = paymentId;
                allocationCommand.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = request.InvoiceId;
                allocationCommand.Parameters.Add("@AmountAllocated", System.Data.SqlDbType.Decimal).Value = request.Amount;
                await allocationCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateInvoiceStatusSql = """
                UPDATE bill.Invoices
                SET StatusCode = CASE
                    WHEN (SELECT Balance FROM bill.vInvoiceBalance WHERE InvoiceId = @InvoiceId) <= 0 THEN 'PAID'
                    WHEN (SELECT Balance FROM bill.vInvoiceBalance WHERE InvoiceId = @InvoiceId) < @TotalInclTax THEN 'PARTIALLY_PAID'
                    ELSE StatusCode
                END
                WHERE InvoiceId = @InvoiceId;
                """;

            await using (var statusCommand = new SqlCommand(updateInvoiceStatusSql, connection, (SqlTransaction)transaction))
            {
                statusCommand.Parameters.Add("@InvoiceId", System.Data.SqlDbType.Int).Value = request.InvoiceId;
                statusCommand.Parameters.Add("@TotalInclTax", System.Data.SqlDbType.Decimal).Value = totalInclTax;
                await statusCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
