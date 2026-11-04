namespace FaultlineDispatcher;

/// <summary>
/// Host-level overrides for the responses FaultlineDispatcher returns. Bound from the
/// <c>Faultline</c> configuration section.
/// </summary>
public sealed class FaultlineOptions
{
    /// <summary>The configuration section the options are bound from.</summary>
    public const string SectionName = "Faultline";

    /// <summary>
    /// Response definitions keyed by response key (see <see cref="FaultlineResponseKeys"/>). An entry overrides
    /// the library default for that key, field by field, or defines a new host-specific key.
    /// </summary>
    public Dictionary<string, ErrorResponse> Responses { get; } = new(StringComparer.OrdinalIgnoreCase);
}
