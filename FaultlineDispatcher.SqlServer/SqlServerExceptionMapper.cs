namespace FaultlineDispatcher.SqlServer;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

/// <summary>
/// Maps <see cref="SqlException"/> error numbers to FaultlineDispatcher response keys. Only the error number is
/// used; the SQL message, constraint, table, column and procedure names never reach the caller. The failure itself
/// is logged by <see cref="GlobalExceptionHandler"/>, and persisted by the database procedure's own error logging.
/// </summary>
public sealed class SqlServerExceptionMapper : IExceptionMapper
{
    private static readonly Dictionary<int, string> BuiltIn = BuildDefaults();

    private readonly SqlServerFaultlineOptions _options;

    /// <summary>Creates the mapper. <paramref name="options"/> may be null to use the defaults.</summary>
    public SqlServerExceptionMapper(IOptions<SqlServerFaultlineOptions>? options = null)
    {
        _options = options?.Value ?? new SqlServerFaultlineOptions();
    }

    /// <inheritdoc />
    public string? Map(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is not SqlException sqlException)
        {
            return null;
        }

        var key = ResolveKey(sqlException.Number);
        if (key is not null)
        {
            return key;
        }

        foreach (SqlError error in sqlException.Errors)
        {
            key = ResolveKey(error.Number);
            if (key is not null)
            {
                return key;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the response key for a SQL error number, or <see langword="null"/> when the number is not mapped
    /// (the handler then answers with the Unexpected response).
    /// </summary>
    public string? ResolveKey(int errorNumber)
    {
        if (_options.Errors.TryGetValue(errorNumber, out var configured) && !string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        if (BuiltIn.TryGetValue(errorNumber, out var builtIn))
        {
            return builtIn;
        }

        if (errorNumber >= _options.ApplicationErrorRangeStart && errorNumber <= _options.ApplicationErrorRangeEnd)
        {
            return _options.ApplicationErrorKey;
        }

        return null;
    }

    private static Dictionary<int, string> BuildDefaults()
    {
        var map = new Dictionary<int, string>
        {
            // Duplicate key (unique index / unique constraint).
            [2601] = FaultlineResponseKeys.Conflict,
            [2627] = FaultlineResponseKeys.Conflict,

            // Deadlock victim: the caller can retry.
            [1205] = FaultlineResponseKeys.Unavailable,

            // Command timeout.
            [-2] = FaultlineResponseKeys.DependencyTimeout
        };

        // Server unreachable, database unavailable, throttling and failover.
        foreach (var number in new[] { 53, 233, 4060, 10928, 10929, 40197, 40501, 40613, 49918, 49919, 49920 })
        {
            map[number] = FaultlineResponseKeys.Unavailable;
        }

        return map;
    }
}
