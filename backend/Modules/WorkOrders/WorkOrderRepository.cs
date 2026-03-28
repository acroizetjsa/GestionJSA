using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Db;
using Gmao.Api.Common.Security;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.WorkOrders;

public sealed class WorkOrderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public WorkOrderRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<WorkOrderSummaryDto>> GetAccessibleAsync(CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var sql = currentUser.RoleCode == Roles.Admin
            ? AdminSql
            : TechnicianSql;

        var results = new List<WorkOrderSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        if (currentUser.RoleCode != Roles.Admin)
        {
            command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new WorkOrderSummaryDto(
                reader.GetRequiredInt32("WorkOrderId"),
                reader.GetRequiredString("WorkOrderNumber"),
                reader.GetRequiredString("TypeCode"),
                reader.GetRequiredString("StatusCode"),
                reader.GetRequiredString("PriorityCode"),
                reader.GetRequiredString("ClientName"),
                reader.GetNullableString("SiteName"),
                reader.GetNullableString("BusLabel"),
                reader.GetRequiredString("Title"),
                reader.GetNullableString("AssignedTechnician"),
                reader.GetRequiredDateTime("ReportedAtUtc"),
                reader.GetNullableDateTime("ScheduledStartUtc"),
                reader.GetNullableDateTime("ClosedAtUtc")));
        }

        return results;
    }

    public async Task<int> CreateAsync(CreateWorkOrderRequest request, int openedByUserId, CancellationToken cancellationToken)
    {
        const string sql = """
            DECLARE @Sequence INT = NEXT VALUE FOR ops.WorkOrderNumberSeq;
            DECLARE @WorkOrderNumber NVARCHAR(30) = CONCAT('WO-', YEAR(SYSUTCDATETIME()), '-', RIGHT(REPLICATE('0', 6) + CAST(@Sequence AS VARCHAR(6)), 6));

            INSERT INTO ops.WorkOrders
            (
                WorkOrderNumber, TypeCode, StatusCode, PriorityCode, ClientId, SiteId, BusId, EquipmentId,
                Title, Description, ScheduledStartUtc, ScheduledEndUtc, AssignedTechnicianId, OpenedByUserId
            )
            VALUES
            (
                @WorkOrderNumber, @TypeCode, 'OPEN', @PriorityCode, @ClientId, @SiteId, @BusId, @EquipmentId,
                @Title, @Description, @ScheduledStartUtc, @ScheduledEndUtc, @AssignedTechnicianId, @OpenedByUserId
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TypeCode", System.Data.SqlDbType.NVarChar, 20).Value = request.TypeCode.Trim().ToUpperInvariant();
        command.Parameters.Add("@PriorityCode", System.Data.SqlDbType.NVarChar, 20).Value = request.PriorityCode.Trim().ToUpperInvariant();
        command.Parameters.Add("@ClientId", System.Data.SqlDbType.Int).Value = request.ClientId;
        command.Parameters.Add("@SiteId", System.Data.SqlDbType.Int).Value = (object?)request.SiteId ?? DBNull.Value;
        command.Parameters.Add("@BusId", System.Data.SqlDbType.Int).Value = (object?)request.BusId ?? DBNull.Value;
        command.Parameters.Add("@EquipmentId", System.Data.SqlDbType.Int).Value = (object?)request.EquipmentId ?? DBNull.Value;
        command.Parameters.Add("@Title", System.Data.SqlDbType.NVarChar, 150).Value = request.Title.Trim();
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, -1).Value = (object?)request.Description ?? DBNull.Value;
        command.Parameters.Add("@ScheduledStartUtc", System.Data.SqlDbType.DateTime2).Value = (object?)request.ScheduledStartUtc ?? DBNull.Value;
        command.Parameters.Add("@ScheduledEndUtc", System.Data.SqlDbType.DateTime2).Value = (object?)request.ScheduledEndUtc ?? DBNull.Value;
        command.Parameters.Add("@AssignedTechnicianId", System.Data.SqlDbType.Int).Value = (object?)request.AssignedTechnicianId ?? DBNull.Value;
        command.Parameters.Add("@OpenedByUserId", System.Data.SqlDbType.Int).Value = openedByUserId;

        var workOrderId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(workOrderId);
    }

    public async Task CloseAsync(int workOrderId, CloseWorkOrderRequest request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var sql = currentUser.RoleCode == Roles.Admin
            ? AdminCloseSql
            : TechnicianCloseSql;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = workOrderId;
        command.Parameters.Add("@ResolutionNotes", System.Data.SqlDbType.NVarChar, -1).Value = (object?)request.ResolutionNotes ?? DBNull.Value;
        command.Parameters.Add("@LaborMinutes", System.Data.SqlDbType.Int).Value = request.LaborMinutes;
        command.Parameters.Add("@TravelKm", System.Data.SqlDbType.Decimal).Value = request.TravelKm;
        if (currentUser.RoleCode != Roles.Admin)
        {
            command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
        }

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
        {
            throw new UnauthorizedAccessException("Vous ne pouvez pas clôturer cet ordre de travail.");
        }
    }

    public async Task ConsumePartAsync(int workOrderId, ConsumePartRequest request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var hasAccess = await CheckAccessAsync(connection, transaction, workOrderId, currentUser, cancellationToken);
            if (!hasAccess)
            {
                throw new UnauthorizedAccessException("Vous ne pouvez pas consommer de pièces sur cet ordre de travail.");
            }

            var unitCost = await GetPartPurchasePriceAsync(connection, transaction, request.PartId, cancellationToken);
            var quantityOnHand = await GetStockOnHandAsync(connection, transaction, request.PartId, request.WarehouseId, cancellationToken);
            if (quantityOnHand < request.Quantity)
            {
                throw new InvalidOperationException("Stock insuffisant pour cette pièce.");
            }

            const string insertWorkOrderPartSql = """
                INSERT INTO ops.WorkOrderParts
                (
                    WorkOrderId, PartId, WarehouseId, Quantity, UnitCost, UnitSalePrice, ConsumedByUserId
                )
                VALUES
                (
                    @WorkOrderId, @PartId, @WarehouseId, @Quantity, @UnitCost, @UnitSalePrice, @ConsumedByUserId
                );
                """;

            await using (var insertCommand = new SqlCommand(insertWorkOrderPartSql, connection, (SqlTransaction)transaction))
            {
                insertCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = workOrderId;
                insertCommand.Parameters.Add("@PartId", System.Data.SqlDbType.Int).Value = request.PartId;
                insertCommand.Parameters.Add("@WarehouseId", System.Data.SqlDbType.Int).Value = request.WarehouseId;
                insertCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Decimal).Value = request.Quantity;
                insertCommand.Parameters.Add("@UnitCost", System.Data.SqlDbType.Decimal).Value = unitCost;
                insertCommand.Parameters.Add("@UnitSalePrice", System.Data.SqlDbType.Decimal).Value = request.UnitSalePrice;
                insertCommand.Parameters.Add("@ConsumedByUserId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string insertStockMovementSql = """
                INSERT INTO stock.StockMovements
                (
                    PartId, WarehouseId, WorkOrderId, MovementTypeCode, Quantity, UnitCost, Reference, PerformedByUserId
                )
                VALUES
                (
                    @PartId, @WarehouseId, @WorkOrderId, 'OUT', @Quantity, @UnitCost, @Reference, @PerformedByUserId
                );
                """;

            await using (var stockCommand = new SqlCommand(insertStockMovementSql, connection, (SqlTransaction)transaction))
            {
                stockCommand.Parameters.Add("@PartId", System.Data.SqlDbType.Int).Value = request.PartId;
                stockCommand.Parameters.Add("@WarehouseId", System.Data.SqlDbType.Int).Value = request.WarehouseId;
                stockCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = workOrderId;
                stockCommand.Parameters.Add("@Quantity", System.Data.SqlDbType.Decimal).Value = request.Quantity;
                stockCommand.Parameters.Add("@UnitCost", System.Data.SqlDbType.Decimal).Value = unitCost;
                stockCommand.Parameters.Add("@Reference", System.Data.SqlDbType.NVarChar, 120).Value = $"WO-{workOrderId}";
                stockCommand.Parameters.Add("@PerformedByUserId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
                await stockCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            const string updateWorkOrderStatusSql = """
                UPDATE ops.WorkOrders
                SET StatusCode = CASE WHEN StatusCode IN ('OPEN', 'PLANNED') THEN 'IN_PROGRESS' ELSE StatusCode END
                WHERE WorkOrderId = @WorkOrderId;
                """;

            await using (var statusCommand = new SqlCommand(updateWorkOrderStatusSql, connection, (SqlTransaction)transaction))
            {
                statusCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = workOrderId;
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

    private static async Task<bool> CheckAccessAsync(SqlConnection connection, System.Data.Common.DbTransaction transaction, int workOrderId, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var sql = currentUser.RoleCode == Roles.Admin
            ? "SELECT COUNT(1) FROM ops.WorkOrders WHERE WorkOrderId = @WorkOrderId;"
            : "SELECT COUNT(1) FROM ops.WorkOrders WHERE WorkOrderId = @WorkOrderId AND AssignedTechnicianId = @UserId;";

        await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
        command.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = workOrderId;
        if (currentUser.RoleCode != Roles.Admin)
        {
            command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) > 0;
    }

    private static async Task<decimal> GetPartPurchasePriceAsync(SqlConnection connection, System.Data.Common.DbTransaction transaction, int partId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT PurchasePriceExclTax FROM stock.Parts WHERE PartId = @PartId;";
        await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
        command.Parameters.Add("@PartId", System.Data.SqlDbType.Int).Value = partId;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null)
        {
            throw new KeyNotFoundException("Pièce introuvable.");
        }

        return Convert.ToDecimal(result);
    }

    private static async Task<decimal> GetStockOnHandAsync(SqlConnection connection, System.Data.Common.DbTransaction transaction, int partId, int warehouseId, CancellationToken cancellationToken)
    {
        const string sql = "SELECT COALESCE(QuantityOnHand, 0) FROM stock.vCurrentStock WHERE PartId = @PartId AND WarehouseId = @WarehouseId;";
        await using var command = new SqlCommand(sql, connection, (SqlTransaction)transaction);
        command.Parameters.Add("@PartId", System.Data.SqlDbType.Int).Value = partId;
        command.Parameters.Add("@WarehouseId", System.Data.SqlDbType.Int).Value = warehouseId;
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDecimal(result ?? 0m);
    }

    private const string BaseSelect = """
        SELECT
            wo.WorkOrderId,
            wo.WorkOrderNumber,
            wo.TypeCode,
            wo.StatusCode,
            wo.PriorityCode,
            c.Name AS ClientName,
            s.Name AS SiteName,
            CASE WHEN b.BusId IS NULL THEN NULL ELSE CONCAT(b.FleetNumber, ' / ', COALESCE(b.RegistrationNumber, '')) END AS BusLabel,
            wo.Title,
            u.FullName AS AssignedTechnician,
            wo.ReportedAtUtc,
            wo.ScheduledStartUtc,
            wo.ClosedAtUtc
        FROM ops.WorkOrders wo
        INNER JOIN crm.Clients c ON c.ClientId = wo.ClientId
        LEFT JOIN crm.Sites s ON s.SiteId = wo.SiteId
        LEFT JOIN asset.Buses b ON b.BusId = wo.BusId
        LEFT JOIN sec.Users u ON u.UserId = wo.AssignedTechnicianId
        """;

    private static readonly string AdminSql = $"{BaseSelect} ORDER BY wo.ReportedAtUtc DESC;";
    private static readonly string TechnicianSql = $"{BaseSelect} WHERE wo.AssignedTechnicianId = @UserId ORDER BY wo.ReportedAtUtc DESC;";

    private const string AdminCloseSql = """
        UPDATE ops.WorkOrders
        SET StatusCode = 'DONE',
            ResolutionNotes = @ResolutionNotes,
            LaborMinutes = @LaborMinutes,
            TravelKm = @TravelKm,
            ClosedAtUtc = SYSUTCDATETIME()
        WHERE WorkOrderId = @WorkOrderId;
        """;

    private const string TechnicianCloseSql = """
        UPDATE ops.WorkOrders
        SET StatusCode = 'DONE',
            ResolutionNotes = @ResolutionNotes,
            LaborMinutes = @LaborMinutes,
            TravelKm = @TravelKm,
            ClosedAtUtc = SYSUTCDATETIME()
        WHERE WorkOrderId = @WorkOrderId
          AND AssignedTechnicianId = @UserId;
        """;
}
