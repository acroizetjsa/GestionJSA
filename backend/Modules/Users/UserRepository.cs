using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Users;

public sealed class UserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public UserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<UserSummaryDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UserId, Username, FullName, Email, Phone, RoleCode, IsActive, CreatedAtUtc, LastLoginAtUtc
            FROM sec.Users
            ORDER BY FullName;
            """;

        var results = new List<UserSummaryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new UserSummaryDto(
                reader.GetRequiredInt32("UserId"),
                reader.GetRequiredString("Username"),
                reader.GetRequiredString("FullName"),
                reader.GetNullableString("Email"),
                reader.GetNullableString("Phone"),
                reader.GetRequiredString("RoleCode"),
                reader.GetRequiredBoolean("IsActive"),
                reader.GetRequiredDateTime("CreatedAtUtc"),
                reader.GetNullableDateTime("LastLoginAtUtc")));
        }

        return results;
    }

    public async Task<int> CreateAsync(CreateUserRequest request, string passwordHash, string passwordSalt, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO sec.Users (Username, PasswordHash, PasswordSalt, FullName, Email, Phone, RoleCode, IsActive)
            VALUES (@Username, @PasswordHash, @PasswordSalt, @FullName, @Email, @Phone, @RoleCode, @IsActive);
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Username", System.Data.SqlDbType.NVarChar, 50).Value = request.Username.Trim();
        command.Parameters.Add("@PasswordHash", System.Data.SqlDbType.NVarChar, 256).Value = passwordHash;
        command.Parameters.Add("@PasswordSalt", System.Data.SqlDbType.NVarChar, 128).Value = passwordSalt;
        command.Parameters.Add("@FullName", System.Data.SqlDbType.NVarChar, 120).Value = request.FullName.Trim();
        command.Parameters.Add("@Email", System.Data.SqlDbType.NVarChar, 120).Value = (object?)request.Email?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@Phone", System.Data.SqlDbType.NVarChar, 40).Value = (object?)request.Phone?.Trim() ?? DBNull.Value;
        command.Parameters.Add("@RoleCode", System.Data.SqlDbType.NVarChar, 20).Value = request.RoleCode.Trim().ToUpperInvariant();
        command.Parameters.Add("@IsActive", System.Data.SqlDbType.Bit).Value = request.IsActive;

        var userId = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(userId);
    }
}
