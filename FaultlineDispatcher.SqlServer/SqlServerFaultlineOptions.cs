namespace FaultlineDispatcher.SqlServer;

/// <summary>
/// Options for <see cref="SqlServerExceptionMapper"/>. Bound from the <c>Faultline:SqlServer</c> configuration section.
/// </summary>
public sealed class SqlServerFaultlineOptions
{
    /// <summary>The configuration section the options are bound from.</summary>
    public const string SectionName = "Faultline:SqlServer";

    /// <summary>
    /// SQL error number to response key. Entries take precedence over every built-in mapping, so a host can map
    /// its own <c>THROW</c> numbers, for example <c>"60073": "ContentNotFound"</c>.
    /// </summary>
    public Dictionary<int, string> Errors { get; } = [];

    /// <summary>First error number of the application range raised with <c>THROW</c>. Defaults to 60000.</summary>
    public int ApplicationErrorRangeStart { get; set; } = 60000;

    /// <summary>Last error number of the application range raised with <c>THROW</c>. Defaults to 60199.</summary>
    public int ApplicationErrorRangeEnd { get; set; } = 60199;

    /// <summary>
    /// Response key for application-range errors that have no entry in <see cref="Errors"/>. Defaults to
    /// <see cref="FaultlineResponseKeys.BadRequest"/>. The <c>THROW</c> message text is never returned to the caller.
    /// </summary>
    public string ApplicationErrorKey { get; set; } = FaultlineResponseKeys.BadRequest;
}
