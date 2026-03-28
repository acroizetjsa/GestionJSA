using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Purchasing;

public sealed class PurchaseOrderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PurchaseOrderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PurchaseOrderSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT po.PurchaseOrderId, po.PurchaseOrderNumber, sup.Name AS SupplierName, s.Name AS SiteName,
                   po.StatusCode, po.OrderedAtUtc, po.ExpectedAtUtc,
                   COALESCE(SUM(pol.QuantityOrdered * pol.UnitPriceExclTax), 0) AS TotalExclTax,
                   COALESCE(SUM(pol.QuantityOrdered * pol.UnitPriceExclTax * (1 + pol.TaxRate / 100.0)), 0) AS TotalInclTax
            FROM buy.PurchaseOrders po
            INNER JOIN buy.Suppliers sup ON sup.SupplierId = po.SupplierId
            LEFT JOIN crm.Sites s ON s.SiteId = po.SiteId
            LEFT JOIN buy.PurchaseOrderLines pol ON pol.PurchaseOrderId = po.PurchaseOrderId
            GROUP BY po.PurchaseOrderId, po.PurchaseOrderNumber, sup.Name, s.Name, po.StatusCode, po.OrderedAtUtc, po.ExpectedAtUtc
            ORDER BY po.OrderedAtUtc DESC;
            """;

        var results = new List<PurchaseOrderSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PurchaseOrderSummaryDto(
                reader.GetRequiredInt32("PurchaseOrderId"),
                reader.GetRequiredString("PurchaseOrderNumber"),
                reader.GetRequiredString("SupplierName"),
                reader.GetNullableString("SiteName"),
                reader.GetRequiredString("StatusCode"),
                reader.GetRequiredDateTime("OrderedAtUtc"),
                reader.GetNullableDateTime("ExpectedAtUtc"),
                reader.GetRequiredDecimal("TotalExclTax"),
                reader.GetRequiredDecimal("TotalInclTax")));
        }

        return results;
    }

    public async Task<int> CreateAsync(CreatePurchaseOrderRequest request, int createdByUserId, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Une commande d'achat doit contenir au moins une ligne.");
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            const string insertHeaderSql = """
                DECLARE @Sequence INT = NEXT VALUE FOR buy.PurchaseOrderNumberSeq;
                DECLARE @PurchaseOrderNumber NVARCHAR(30) = CONCAT('PO-', YEAR(SYSUTCDATETIME()), '-', RIGHT(REPLICATE('0', 6) + CAST(@Sequence AS VARCHAR(6)), 6));

                INSERT INTO buy.PurchaseOrders
                (
                    PurchaseOrderNumber, SupplierId, SiteId, StatusCode, ExpectedAtUtc, Notes, CreatedByUserId
                )
                VALUES
                (
                    @PurchaseOrderNumber, @SupplierId, @SiteId, 'ORDERED', @ExpectedAtUtc, @Notes, @CreatedByUserId
                );

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            int purchaseOrderId;
            await using (var headerCommand = new SqlCommand(insertHeaderSql, connection, (SqlTransaction)transaction))
            {
                headerCommand.Parameters.Add("@SupplierId", System.Data.SqlDbType.Int).Value = request.SupplierId;
                headerCommand.Parameters.Add("@SiteId", System.Data.SqlDbType.Int).Value = (object?)request.SiteId ?? DBNull.Value;
                headerCommand.Parameters.Add("@ExpectedAtUtc", System.Data.SqlDbType.DateTime2).Value = (object?)request.ExpectedAtUtc ?? DBNull.Value;
                headerCommand.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 2000).Value = (object?)request.Notes ?? DBNull.Value;
                headerCommand.Parameters.Add("@CreatedByUserId", System.Data.SqlDbType.Int).Value = createdByUserId;
                purchaseOrderId = Convert.ToInt32(await headerCommand.ExecuteScalarAsync(cancellationToken));
            }

            const string insertLineSql = """
                INSERT INTO buy.PurchaseOrderLines
                (
                    PurchaseOrderId, PartId, QuantityOrdered, UnitPriceExclTax, TaxRate
                )
                VALUES
                (
                    @PurchaseOrderId, @PartId, @QuantityOrdered, @UnitPriceExclTax, @TaxRate
                );
                """;

            foreach (var line in request.Lines)
            {
                await using var lineCommand = new SqlCommand(insertLineSql, connection, (SqlTransaction)transaction);
                lineCommand.Parameters.Add("@PurchaseOrderId", System.Data.SqlDbType.Int).Value = purchaseOrderId;
                lineCommand.Parameters.Add("@PartId", System.Data.SqlDbType.Int).Value = line.PartId;
                lineCommand.Parameters.Add("@QuantityOrdered", System.Data.SqlDbType.Decimal).Value = line.QuantityOrdered;
                lineCommand.Parameters.Add("@UnitPriceExclTax", System.Data.SqlDbType.Decimal).Value = line.UnitPriceExclTax;
                lineCommand.Parameters.Add("@TaxRate", System.Data.SqlDbType.Decimal).Value = line.TaxRate;
                await lineCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
            return purchaseOrderId;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task ReceiveAsync(int purchaseOrderId, ReceivePurchaseOrderRequest request, int performedByUserId, CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
        {
            throw new InvalidOperationException("Aucune ligne de réception transmise.");
        }

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var line in request.Lines)
            {
                const string selectLineSql = """
                    SELECT pol.PartId, pol.UnitPriceExclTax
                    FROM buy.PurchaseOrderLines pol
                    WHERE pol.PurchaseOrderLineId = @PurchaseOrderLineId
                      AND pol.PurchaseOrderId = @PurchaseOrderId;
                    """;

                int partId;
                decimal unitCost;
                await using (var selectCommand = new SqlCommand(selectLineSql, connection, (SqlTransaction)transaction))
                {
                    selectCommand.Parameters.Add("@PurchaseOrderLineId", System.Data.SqlDbType.Int).Value = line.PurchaseOrderLineId;
                    selectCommand.Parameters.Add("@PurchaseOrderId", System.Data.SqlDbType.Int).Value = purchaseOrderId;
                    await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
                    if (!await reader.ReadAsync(cancellationToken))
                    {
                        throw new KeyNotFoundException("Ligne de commande introuvable.");
                    }

                    partId = reader.GetRequiredInt32("PartId");
                    unitCost = reader.GetRequiredDecimal("UnitPriceExclTax");
                }

                const string updateLineSql = """
                    UPDATE buy.PurchaseOrderLines
                    SET QuantityReceived = QuantityReceived + @QuantityReceived
                    WHERE PurchaseOrderLineId = @PurchaseOrderLineId
                      AND PurchaseOrderId = @PurchaseOrderId;
                    """;

                await using (var updateCommand = new SqlCommand(updateLineSql, connection, (SqlTransaction)transaction))
                {
                    updateCommand.Parameters.Add("@QuantityReceived", System.Data.SqlDbType.Decimal).Value = line.QuantityReceived;
                    updateCommand.Parameters.Add("@PurchaseOrderLineId", System.Data.SqlDbType.Int).Value = line.PurchaseOrderLineId;
                    updateCommand.Parameters.Add("@PurchaseOrderId", System.Data.SqlDbType.Int).Value = purchaseOrderId;
                    await updateCommand.ExecuteNonQueryAsync(cancellationToken);
                }

                const string stockSql = """
                    INSERT INTO stock.StockMovements
                    (
                        PartId, WarehouseId, PurchaseOrderLineId, MovementTypeCode, Quantity, UnitCost, Reference, PerformedByUserId
                    )
                    VALUES
                    (
                        @PartId, @WarehouseId, @PurchaseOrderLineId, 'IN', @Quantity, @UnitCost, @Reference, @PerformedByUserId
                    );
                    """;

                await using (var stockCommand = new SqlCommand(stockSql, connection, (SqlTransaction)transaction))
                {
                    stockCommand.Parameters.Add("@PartId", System.Data.SqlDbType.Int).Value = partId;
                    stockCommand.Parameters.Add("@WarehouseId", System.Data.SqlDbType.Int).Value = line.WarehouseId;
                    stockCommand.Parameters.Add("@PurchaseOrderLineId", System.Data.SqlDbType.Int).Value = line.PurchaseOrderLineId;
                    stockCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Decimal).Value = line.QuantityReceived;
                    stockCommand.Parameters.Add("@UnitCost", System.Data.SqlDbType.Decimal).Value = unitCost;
                    stockCommand.Parameters.Add("@Reference", System.Data.SqlDbType.NVarChar, 120).Value = $"PO-{purchaseOrderId}";
                    stockCommand.Parameters.Add("@PerformedByUserId", System.Data.SqlDbType.Int).Value = performedByUserId;
                    await stockCommand.ExecuteNonQueryAsync(cancellationToken);
                }
            }

            const string updateHeaderSql = """
                UPDATE buy.PurchaseOrders
                SET StatusCode = CASE
                    WHEN EXISTS (
                        SELECT 1
                        FROM buy.PurchaseOrderLines pol
                        WHERE pol.PurchaseOrderId = @PurchaseOrderId
                          AND pol.QuantityReceived < pol.QuantityOrdered
                    ) THEN 'PARTIALLY_RECEIVED'
                    ELSE 'RECEIVED'
                END
                WHERE PurchaseOrderId = @PurchaseOrderId;
                """;

            await using (var statusCommand = new SqlCommand(updateHeaderSql, connection, (SqlTransaction)transaction))
            {
                statusCommand.Parameters.Add("@PurchaseOrderId", System.Data.SqlDbType.Int).Value = purchaseOrderId;
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
