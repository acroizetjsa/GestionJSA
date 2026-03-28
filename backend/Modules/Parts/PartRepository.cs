using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Parts;

public sealed class PartRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public PartRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<PartSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT p.PartId, p.PartNumber, p.Name, p.UnitCode, p.PurchasePriceExclTax, p.SalePriceExclTax,
                   p.MinimumStock, COALESCE(SUM(v.QuantityOnHand), 0) AS QuantityOnHand
            FROM stock.Parts p
            LEFT JOIN stock.vCurrentStock v ON v.PartId = p.PartId
            GROUP BY p.PartId, p.PartNumber, p.Name, p.UnitCode, p.PurchasePriceExclTax, p.SalePriceExclTax, p.MinimumStock
            ORDER BY p.Name;
            """;

        var results = new List<PartSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new PartSummaryDto(
                reader.GetRequiredInt32("PartId"),
                reader.GetRequiredString("PartNumber"),
                reader.GetRequiredString("Name"),
                reader.GetRequiredString("UnitCode"),
                reader.GetRequiredDecimal("PurchasePriceExclTax"),
                reader.GetRequiredDecimal("SalePriceExclTax"),
                reader.GetRequiredDecimal("MinimumStock"),
                reader.GetRequiredDecimal("QuantityOnHand")));
        }

        return results;
    }

    public async Task<int> CreateAsync(CreatePartRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO stock.Parts
            (
                PartNumber, Name, Description, UnitCode, PurchasePriceExclTax, SalePriceExclTax, MinimumStock, IsActive
            )
            VALUES
            (
                @PartNumber, @Name, @Description, @UnitCode, @PurchasePriceExclTax, @SalePriceExclTax, @MinimumStock, @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@PartNumber", System.Data.SqlDbType.NVarChar, 50).Value = request.PartNumber.Trim().ToUpperInvariant();
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 150).Value = request.Name.Trim();
        command.Parameters.Add("@Description", System.Data.SqlDbType.NVarChar, 4000).Value = (object?)request.Description ?? DBNull.Value;
        command.Parameters.Add("@UnitCode", System.Data.SqlDbType.NVarChar, 20).Value = request.UnitCode.Trim().ToUpperInvariant();
        command.Parameters.Add("@PurchasePriceExclTax", System.Data.SqlDbType.Decimal).Value = request.PurchasePriceExclTax;
        command.Parameters.Add("@SalePriceExclTax", System.Data.SqlDbType.Decimal).Value = request.SalePriceExclTax;
        command.Parameters.Add("@MinimumStock", System.Data.SqlDbType.Decimal).Value = request.MinimumStock;
        command.Parameters.Add("@IsActive", System.Data.SqlDbType.Bit).Value = request.IsActive;
        var partId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(partId);
    }
}
