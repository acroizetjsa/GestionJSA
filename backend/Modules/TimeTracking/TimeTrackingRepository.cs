using Gmao.Api.Common.Auth;
using Gmao.Api.Common.Db;
using Gmao.Api.Common.Security;
using Microsoft.Data.SqlClient;

namespace Gmao.Api.Modules.TimeTracking;

public sealed class TimeTrackingRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public TimeTrackingRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IReadOnlyList<TimeEntryDto>> GetAccessibleAsync(CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var sql = currentUser.RoleCode == Roles.Admin
            ? AdminSql
            : TechnicianSql;

        var results = new List<TimeEntryDto>();
        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        if (currentUser.RoleCode != Roles.Admin)
        {
            command.Parameters.Add("@UserId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new TimeEntryDto(
                reader.GetRequiredInt32("TimeEntryId"),
                reader.GetRequiredInt32("TechnicianId"),
                reader.GetNullableInt32("WorkOrderId"),
                reader.GetRequiredDateTime("StartAtUtc"),
                reader.GetNullableDateTime("EndAtUtc"),
                reader.GetRequiredString("StatusCode"),
                reader.GetNullableDecimal("StartLatitude"),
                reader.GetNullableDecimal("StartLongitude"),
                reader.GetNullableDecimal("EndLatitude"),
                reader.GetNullableDecimal("EndLongitude"),
                reader.GetNullableString("Notes")));
        }

        return results;
    }

    public async Task<int> StartAsync(StartTimeEntryRequest request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        const string checkOpenSql = "SELECT COUNT(1) FROM ops.TimeEntries WHERE TechnicianId = @TechnicianId AND StatusCode = 'OPEN';";
        const string insertSql = """
            INSERT INTO ops.TimeEntries
            (
                TechnicianId, WorkOrderId, StartAtUtc, StartLatitude, StartLongitude, StartAccuracyMeters, Notes, StatusCode
            )
            VALUES
            (
                @TechnicianId, @WorkOrderId, SYSUTCDATETIME(), @StartLatitude, @StartLongitude, @StartAccuracyMeters, @Notes, 'OPEN'
            );
            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);

        await using (var checkCommand = new SqlCommand(checkOpenSql, connection))
        {
            checkCommand.Parameters.Add("@TechnicianId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
            var openCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync(cancellationToken));
            if (openCount > 0)
            {
                throw new InvalidOperationException("Un pointage est déjà ouvert pour ce technicien.");
            }
        }

        await using var insertCommand = new SqlCommand(insertSql, connection);
        insertCommand.Parameters.Add("@TechnicianId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
        insertCommand.Parameters.Add("@WorkOrderId", System.Data.SqlDbType.Int).Value = (object?)request.WorkOrderId ?? DBNull.Value;
        insertCommand.Parameters.Add("@StartLatitude", System.Data.SqlDbType.Decimal).Value = (object?)request.StartLatitude ?? DBNull.Value;
        insertCommand.Parameters.Add("@StartLongitude", System.Data.SqlDbType.Decimal).Value = (object?)request.StartLongitude ?? DBNull.Value;
        insertCommand.Parameters.Add("@StartAccuracyMeters", System.Data.SqlDbType.Decimal).Value = (object?)request.StartAccuracyMeters ?? DBNull.Value;
        insertCommand.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 2000).Value = (object?)request.Notes ?? DBNull.Value;
        var timeEntryId = await insertCommand.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(timeEntryId);
    }

    public async Task StopAsync(int timeEntryId, StopTimeEntryRequest request, CurrentUser currentUser, CancellationToken cancellationToken)
    {
        var sql = currentUser.RoleCode == Roles.Admin
            ? AdminStopSql
            : TechnicianStopSql;

        await using var connection = await _connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@TimeEntryId", System.Data.SqlDbType.Int).Value = timeEntryId;
        command.Parameters.Add("@EndLatitude", System.Data.SqlDbType.Decimal).Value = (object?)request.EndLatitude ?? DBNull.Value;
        command.Parameters.Add("@EndLongitude", System.Data.SqlDbType.Decimal).Value = (object?)request.EndLongitude ?? DBNull.Value;
        command.Parameters.Add("@EndAccuracyMeters", System.Data.SqlDbType.Decimal).Value = (object?)request.EndAccuracyMeters ?? DBNull.Value;
        if (currentUser.RoleCode != Roles.Admin)
        {
            command.Parameters.Add("@TechnicianId", System.Data.SqlDbType.Int).Value = currentUser.UserId;
        }

        var rows = await command.ExecuteNonQueryAsync(cancellationToken);
        if (rows == 0)
        {
            throw new UnauthorizedAccessException("Vous ne pouvez pas clôturer ce pointage.");
        }
    }

    private const string BaseSql = """
        SELECT TimeEntryId, TechnicianId, WorkOrderId, StartAtUtc, EndAtUtc, StatusCode,
               StartLatitude, StartLongitude, EndLatitude, EndLongitude, Notes
        FROM ops.TimeEntries
        """;

    private static readonly string AdminSql = $"{BaseSql} ORDER BY StartAtUtc DESC;";
    private static readonly string TechnicianSql = $"{BaseSql} WHERE TechnicianId = @UserId ORDER BY StartAtUtc DESC;";

    private const string AdminStopSql = """
        UPDATE ops.TimeEntries
        SET EndAtUtc = SYSUTCDATETIME(),
            EndLatitude = @EndLatitude,
            EndLongitude = @EndLongitude,
            EndAccuracyMeters = @EndAccuracyMeters,
            StatusCode = 'CLOSED'
        WHERE TimeEntryId = @TimeEntryId
          AND StatusCode = 'OPEN';
        """;

    private const string TechnicianStopSql = """
        UPDATE ops.TimeEntries
        SET EndAtUtc = SYSUTCDATETIME(),
            EndLatitude = @EndLatitude,
            EndLongitude = @EndLongitude,
            EndAccuracyMeters = @EndAccuracyMeters,
            StatusCode = 'CLOSED'
        WHERE TimeEntryId = @TimeEntryId
          AND TechnicianId = @TechnicianId
          AND StatusCode = 'OPEN';
        """;
}
