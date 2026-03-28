using Microsoft.Data.SqlClient;

namespace Gmao.Api.Common.Db;

public interface ISqlConnectionFactory
{
    Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<SqlConnection> OpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
