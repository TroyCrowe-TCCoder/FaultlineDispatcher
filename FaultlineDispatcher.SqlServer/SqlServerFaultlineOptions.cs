namespace FaultlineDispatcher.SqlServer;

/// <summary>
/// Options for <see cref="SqlServerExceptionMapper"/>. The host supplies every value in code through
/// <see cref="ServiceCollectionExtensions.AddFaultlineSqlServer"/>; there are no defaults.
/// </summary>
public sealed class SqlServerFaultlineOptions
{
    /// <summary>
    /// SQL error number to response key. Entries take precedence over every built-in mapping, so a host can map
    /// its own <c>THROW</c> numbers, for example <c>60073</c> to <c>"ContentNotFound"</c>. Optional.
    /// </summary>
    public Dictionary<int, string> Errors { get; } = [];

    /// <summary>First error number of the application range raised with <c>THROW</c> (for example 60000). Required.</summary>
    public int ApplicationErrorRangeStart { get; set; }

    /// <summary>Last error number of the application range raised with <c>THROW</c> (for example 60199). Required.</summary>
    public int ApplicationErrorRangeEnd { get; set; }

    /// <summary>
    /// Response key for application-range errors that have no entry in <see cref="Errors"/>, for example
    /// <see cref="FaultlineResponseKeys.BadRequest"/>. Required. The <c>THROW</c> message text is never returned to
    /// the caller.
    /// </summary>
    public string ApplicationErrorKey { get; set; } = string.Empty;
}
