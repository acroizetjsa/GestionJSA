using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Buses;

public sealed class BusRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public BusRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<BusSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT b.BusId, b.FleetNumber, b.RegistrationNumber, b.Vin, b.Brand, b.Model,
                   b.YearOfManufacture, b.CurrentMileageKm, b.StatusCode,
                   c.Name AS ClientName, s.Name AS SiteName
            FROM asset.Buses b
            INNER JOIN crm.Clients c ON c.ClientId = b.ClientId
            LEFT JOIN crm.Sites s ON s.SiteId = b.SiteId
            ORDER BY c.Name, b.FleetNumber;
            """;

        var results = new List<BusSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new BusSummaryDto(
                reader.GetRequiredInt32("BusId"),
                reader.GetRequiredString("FleetNumber"),
                reader.GetNullableString("RegistrationNumber"),
                reader.GetNullableString("Vin"),
                reader.GetNullableString("Brand"),
                reader.GetNullableString("Model"),
                reader.GetNullableInt32("YearOfManufacture"),
                reader.GetRequiredInt32("CurrentMileageKm"),
                reader.GetRequiredString("StatusCode"),
                reader.GetRequiredString("ClientName"),
                reader.GetNullableString("SiteName")));
        }

        return results;
    }

    public async Task<int> CreateAsync(CreateBusRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO asset.Buses
            (
                ClientId, SiteId, FleetNumber, RegistrationNumber, Vin, Brand, Model,
                YearOfManufacture, CurrentMileageKm, StatusCode, Notes
            )
            VALUES
            (
                @ClientId, @SiteId, @FleetNumber, @RegistrationNumber, @Vin, @Brand, @Model,
                @YearOfManufacture, @CurrentMileageKm, @StatusCode, @Notes
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ClientId", System.Data.SqlDbType.Int).Value = request.ClientId;
        command.Parameters.Add("@SiteId", System.Data.SqlDbType.Int).Value = (object?)request.SiteId ?? DBNull.Value;
        command.Parameters.Add("@FleetNumber", System.Data.SqlDbType.NVarChar, 50).Value = request.FleetNumber.Trim().ToUpperInvariant();
        command.Parameters.Add("@RegistrationNumber", System.Data.SqlDbType.NVarChar, 30).Value = (object?)request.RegistrationNumber ?? DBNull.Value;
        command.Parameters.Add("@Vin", System.Data.SqlDbType.NVarChar, 50).Value = (object?)request.Vin ?? DBNull.Value;
        command.Parameters.Add("@Brand", System.Data.SqlDbType.NVarChar, 80).Value = (object?)request.Brand ?? DBNull.Value;
        command.Parameters.Add("@Model", System.Data.SqlDbType.NVarChar, 80).Value = (object?)request.Model ?? DBNull.Value;
        command.Parameters.Add("@YearOfManufacture", System.Data.SqlDbType.Int).Value = (object?)request.YearOfManufacture ?? DBNull.Value;
        command.Parameters.Add("@CurrentMileageKm", System.Data.SqlDbType.Int).Value = request.CurrentMileageKm;
        command.Parameters.Add("@StatusCode", System.Data.SqlDbType.NVarChar, 30).Value = request.StatusCode.Trim().ToUpperInvariant();
        command.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, -1).Value = (object?)request.Notes ?? DBNull.Value;

        var busId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(busId);
    }
}
