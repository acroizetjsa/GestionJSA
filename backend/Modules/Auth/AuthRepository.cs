using Gmao.Api.Common.Db;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.Auth;

public sealed class AuthRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public AuthRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<AuthUserRecord?> FindByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT UserId, Username, PasswordHash, PasswordSalt, FullName, RoleCode, IsActive
            FROM sec.Users
            WHERE Username = @Username;
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Username", System.Data.SqlDbType.NVarChar, 50).Value = username;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthUserRecord(
            reader.GetRequiredInt32("UserId"),
            reader.GetRequiredString("Username"),
            reader.GetRequiredString("PasswordHash"),
            reader.GetRequiredString("PasswordSalt"),
            reader.GetRequiredString("FullName"),
            reader.GetRequiredString("RoleCode"),
            reader.GetRequiredBoolean("IsActive"));
    }

    public async Task UpdateLastLoginAsync(int userId, CancellationToken cancellationToken)
    {
        const string sql = "UPDATE sec.Users SET LastLoginAtUtc = SYSUTCDATETIME() WHERE UserId = @UserId;";

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = userId;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
