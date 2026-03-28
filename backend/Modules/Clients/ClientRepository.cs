using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Clients;

public sealed class ClientRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public ClientRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<ClientSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT c.ClientId, c.Code, c.Name, c.DefaultHourlyRateExclTax, c.PaymentTermDays, c.IsActive,
                   COUNT(CASE WHEN s.IsActive = 1 THEN 1 END) AS ActiveSiteCount
            FROM crm.Clients c
            LEFT JOIN crm.Sites s ON s.ClientId = c.ClientId
            GROUP BY c.ClientId, c.Code, c.Name, c.DefaultHourlyRateExclTax, c.PaymentTermDays, c.IsActive
            ORDER BY c.Name;
            """;

        var results = new List<ClientSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new ClientSummaryDto(
                reader.GetRequiredInt32("ClientId"),
                reader.GetRequiredString("Code"),
                reader.GetRequiredString("Name"),
                reader.GetRequiredDecimal("DefaultHourlyRateExclTax"),
                reader.GetRequiredInt32("PaymentTermDays"),
                reader.GetRequiredBoolean("IsActive"),
                reader.GetRequiredInt32("ActiveSiteCount")));
        }

        return results;
    }

    public async Task<int> CreateClientAsync(CreateClientRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO crm.Clients
            (
                Code, Name, BillingAddressLine1, BillingAddressLine2, BillingPostalCode, BillingCity,
                BillingCountry, VatNumber, PaymentTermDays, DefaultHourlyRateExclTax, IsActive
            )
            VALUES
            (
                @Code, @Name, @BillingAddressLine1, @BillingAddressLine2, @BillingPostalCode, @BillingCity,
                @BillingCountry, @VatNumber, @PaymentTermDays, @DefaultHourlyRateExclTax, @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Code", System.Data.SqlDbType.NVarChar, 30).Value = request.Code.Trim().ToUpperInvariant();
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 150).Value = request.Name.Trim();
        command.Parameters.Add("@BillingAddressLine1", System.Data.SqlDbType.NVarChar, 150).Value = (object?)request.BillingAddressLine1 ?? DBNull.Value;
        command.Parameters.Add("@BillingAddressLine2", System.Data.SqlDbType.NVarChar, 150).Value = (object?)request.BillingAddressLine2 ?? DBNull.Value;
        command.Parameters.Add("@BillingPostalCode", System.Data.SqlDbType.NVarChar, 20).Value = (object?)request.BillingPostalCode ?? DBNull.Value;
        command.Parameters.Add("@BillingCity", System.Data.SqlDbType.NVarChar, 80).Value = (object?)request.BillingCity ?? DBNull.Value;
        command.Parameters.Add("@BillingCountry", System.Data.SqlDbType.NVarChar, 80).Value = (object?)request.BillingCountry ?? DBNull.Value;
        command.Parameters.Add("@VatNumber", System.Data.SqlDbType.NVarChar, 50).Value = (object?)request.VatNumber ?? DBNull.Value;
        command.Parameters.Add("@PaymentTermDays", System.Data.SqlDbType.Int).Value = request.PaymentTermDays;
        command.Parameters.Add("@DefaultHourlyRateExclTax", System.Data.SqlDbType.Decimal).Value = request.DefaultHourlyRateExclTax;
        command.Parameters.Add("@IsActive", System.Data.SqlDbType.Bit).Value = request.IsActive;

        var clientId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(clientId);
    }

    public async Task<IReadOnlyList<SiteSummaryDto>> GetSitesAsync(int clientId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT SiteId, ClientId, Code, Name, City, Country, ContactName, ContactPhone, IsActive
            FROM crm.Sites
            WHERE ClientId = @ClientId
            ORDER BY Name;
            """;

        var results = new List<SiteSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ClientId", System.Data.SqlDbType.Int).Value = clientId;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SiteSummaryDto(
                reader.GetRequiredInt32("SiteId"),
                reader.GetRequiredInt32("ClientId"),
                reader.GetRequiredString("Code"),
                reader.GetRequiredString("Name"),
                reader.GetNullableString("City"),
                reader.GetNullableString("Country"),
                reader.GetNullableString("ContactName"),
                reader.GetNullableString("ContactPhone"),
                reader.GetRequiredBoolean("IsActive")));
        }

        return results;
    }

    public async Task<int> CreateSiteAsync(int clientId, CreateSiteRequest request, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO crm.Sites
            (
                ClientId, Code, Name, AddressLine1, AddressLine2, PostalCode, City, Country,
                Latitude, Longitude, ContactName, ContactPhone, IsActive
            )
            VALUES
            (
                @ClientId, @Code, @Name, @AddressLine1, @AddressLine2, @PostalCode, @City, @Country,
                @Latitude, @Longitude, @ContactName, @ContactPhone, @IsActive
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@ClientId", System.Data.SqlDbType.Int).Value = clientId;
        command.Parameters.Add("@Code", System.Data.SqlDbType.NVarChar, 30).Value = request.Code.Trim().ToUpperInvariant();
        command.Parameters.Add("@Name", System.Data.SqlDbType.NVarChar, 150).Value = request.Name.Trim();
        command.Parameters.Add("@AddressLine1", System.Data.SqlDbType.NVarChar, 150).Value = (object?)request.AddressLine1 ?? DBNull.Value;
        command.Parameters.Add("@AddressLine2", System.Data.SqlDbType.NVarChar, 150).Value = (object?)request.AddressLine2 ?? DBNull.Value;
        command.Parameters.Add("@PostalCode", System.Data.SqlDbType.NVarChar, 20).Value = (object?)request.PostalCode ?? DBNull.Value;
        command.Parameters.Add("@City", System.Data.SqlDbType.NVarChar, 80).Value = (object?)request.City ?? DBNull.Value;
        command.Parameters.Add("@Country", System.Data.SqlDbType.NVarChar, 80).Value = (object?)request.Country ?? DBNull.Value;
        command.Parameters.Add("@Latitude", System.Data.SqlDbType.Decimal).Value = (object?)request.Latitude ?? DBNull.Value;
        command.Parameters.Add("@Longitude", System.Data.SqlDbType.Decimal).Value = (object?)request.Longitude ?? DBNull.Value;
        command.Parameters.Add("@ContactName", System.Data.SqlDbType.NVarChar, 120).Value = (object?)request.ContactName ?? DBNull.Value;
        command.Parameters.Add("@ContactPhone", System.Data.SqlDbType.NVarChar, 40).Value = (object?)request.ContactPhone ?? DBNull.Value;
        command.Parameters.Add("@IsActive", System.Data.SqlDbType.Bit).Value = request.IsActive;

        var siteId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(siteId);
    }
}
