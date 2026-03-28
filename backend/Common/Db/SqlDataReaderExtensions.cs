using Microsoft.Data.SqlClient;

namespace Gmao.Api.Common.Db;

public static class SqlDataReaderExtensions
{
    public static int GetRequiredInt32(this SqlDataReader reader, string columnName)
        => reader.GetInt32(reader.GetOrdinal(columnName));

    public static int? GetNullableInt32(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    public static decimal GetRequiredDecimal(this SqlDataReader reader, string columnName)
        => reader.GetDecimal(reader.GetOrdinal(columnName));

    public static decimal? GetNullableDecimal(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    public static string GetRequiredString(this SqlDataReader reader, string columnName)
        => reader.GetString(reader.GetOrdinal(columnName));

    public static string? GetNullableString(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static bool GetRequiredBoolean(this SqlDataReader reader, string columnName)
        => reader.GetBoolean(reader.GetOrdinal(columnName));

    public static DateTime GetRequiredDateTime(this SqlDataReader reader, string columnName)
        => reader.GetDateTime(reader.GetOrdinal(columnName));

    public static DateTime? GetNullableDateTime(this SqlDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }
}
